/*
    2026-11-23 — PO Sync Case Analysis, remaining items:
      N11  SP_SAVE_PO and SP_SAVE_INSPECTION take a per-PO application lock (QPS_PO_QTY_<POID>) and validate the
           quantity inside the transaction, so two saves for the same PO cannot together exceed the 110% limit.
      N9   SP_UPDATE_POHEADER also syncs the PO line items (MERGE on POID + LINE_ITEM; PO_DETAILS_ID stays stable).
      E12  INSPECTIONS stores the PO quantity, fabric, colour and MRP when the report is completed; the PDF report
           (PRO_INSPECTION_DETAIL / INSPECTION_REPORT_PDF) reads those values. Reports completed before this
           migration are frozen with today's PO values (decision 23-Nov-2026).
      Reset SP_SAVE_INSPECTION allows "Reset by Admin" only for QA Admin (role 1008) (open point 7).

    Run once on each existing QPS database; re-running it is harmless:
        sqlcmd -S <server> -d VMM_QPS -b -i Database/migrations/2026-11-23-po-sync-phase2.sql
*/
SET NOCOUNT ON;
GO
/* E12: snapshot columns */
IF COL_LENGTH('dbo.INSPECTIONS', 'PO_TOTAL_QTY_AT_COMPLETION') IS NULL
    ALTER TABLE dbo.INSPECTIONS ADD PO_TOTAL_QTY_AT_COMPLETION DECIMAL(18, 3) NULL;
IF COL_LENGTH('dbo.INSPECTIONS', 'PO_FABRIC_AT_COMPLETION') IS NULL
    ALTER TABLE dbo.INSPECTIONS ADD PO_FABRIC_AT_COMPLETION VARCHAR(255) NULL;
IF COL_LENGTH('dbo.INSPECTIONS', 'PO_COLOR_AT_COMPLETION') IS NULL
    ALTER TABLE dbo.INSPECTIONS ADD PO_COLOR_AT_COMPLETION VARCHAR(100) NULL;
IF COL_LENGTH('dbo.INSPECTIONS', 'PO_MRP_AT_COMPLETION') IS NULL
    ALTER TABLE dbo.INSPECTIONS ADD PO_MRP_AT_COMPLETION DECIMAL(18, 2) NULL;
GO
/* E12: freeze the reports that are already complete with today's PO values. The inspection audit trigger is
   switched off for this one-off update so it does not add an audit entry for every completed report. */
BEGIN TRY
    BEGIN TRAN;
    DISABLE TRIGGER dbo.TRG_INSPECTION_AUDIT_TRAIL ON dbo.INSPECTIONS;
    UPDATE I
       SET PO_TOTAL_QTY_AT_COMPLETION = P.TOTAL_QTY,
           PO_FABRIC_AT_COMPLETION    = P.FABRIC,
           PO_COLOR_AT_COMPLETION     = P.COLOR,
           PO_MRP_AT_COMPLETION       = P.MRP
      FROM dbo.INSPECTIONS I
      JOIN dbo.INSPECTION_REQUESTS R ON R.INSP_ID = I.INSPECTION_ID
      JOIN dbo.POHEADER P ON P.POID = R.POID
     WHERE I.IS_COMPLETE = 1 AND I.PO_TOTAL_QTY_AT_COMPLETION IS NULL;
    ENABLE TRIGGER dbo.TRG_INSPECTION_AUDIT_TRAIL ON dbo.INSPECTIONS;
    COMMIT;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK;      -- also undoes the DISABLE TRIGGER
    THROW;
END CATCH
GO

SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE OR ALTER PROCEDURE [dbo].[SP_UPDATE_POHEADER]
(
    @POID INT OUTPUT,
    @PO_NUMBER VARCHAR(30),
    @FABRIC VARCHAR(255),
    @COLOR VARCHAR(100),
    @TOTAL_QTY DECIMAL(18,3),
    @APPARELS VARCHAR(50) = NULL,
    @CATEGORY VARCHAR(50) = NULL,
    @MC VARCHAR(50) = NULL,
    @MC_TEXT VARCHAR(250) = NULL,
    @MRP DECIMAL(18,3) = NULL,
    @BUYER_ID VARCHAR(20) = NULL,
    @BRAND_ID VARCHAR(250) = NULL,
    @BRAND_ID2 VARCHAR(250) = NULL,
    @FIT VARCHAR(250) = NULL,
    @VENDOR_NAME VARCHAR(100) = NULL,              -- NEW (E13)
    @BUYER_NAME VARCHAR(50) = NULL,                -- NEW (E13)
    @PO_DETAILS dbo.TVP_PODETAIL READONLY,         -- N9: SAP line items; leave empty to sync the header only
    @USER_ID INT,
    @RECORD_ID INT OUTPUT,
    @ERROR_CODE INT = 0 OUTPUT,
    @ERROR_MESSAGE VARCHAR(500) = 'Success' OUTPUT,
    @WARNING_MESSAGE VARCHAR(500) = NULL OUTPUT    -- NEW (quantity impact warning)
)
AS
BEGIN
    SET NOCOUNT ON;
 
    DECLARE @DB_PO_NUMBER VARCHAR(30), @OLD_TOTAL_QTY DECIMAL(18,3),
            @NORM_DB_PO VARCHAR(30), @NORM_SAP_PO VARCHAR(30),
            @NEW_LIMIT INT = 0, @OPEN_IR_QTY INT = 0, @PASSED_QTY INT = 0, @COMMITTED_QTY INT = 0,
            @LOG_OLD_VALUE VARCHAR(MAX), @LOG_NEW_VALUE VARCHAR(MAX);
 
    BEGIN TRY
        SET @ERROR_CODE = 0;
        SET @ERROR_MESSAGE = 'Success';
        SET @WARNING_MESSAGE = NULL;
        SET @RECORD_ID = 0;
 
        /* 1. The PO must already exist in QPS (N6) */
        IF (ISNULL(@POID, 0) <= 0)
            RAISERROR('PO %s cannot be synchronized because it is not saved in QPS (POID is missing).', 16, 1, @PO_NUMBER);
 
        SELECT @DB_PO_NUMBER = PO_NUMBER, @OLD_TOTAL_QTY = TOTAL_QTY
        FROM POHEADER WHERE POID = @POID;
 
        IF (@DB_PO_NUMBER IS NULL)
            RAISERROR('PO %s was not found in QPS for POID %d.', 16, 1, @PO_NUMBER, @POID);
 
        /* 2. SAP PO number must match the QPS PO number, ignoring spaces and leading zeros (N5) */
        SET @NORM_DB_PO  = LTRIM(RTRIM(@DB_PO_NUMBER));
        SET @NORM_SAP_PO = LTRIM(RTRIM(@PO_NUMBER));
        SET @NORM_DB_PO  = SUBSTRING(@NORM_DB_PO,  PATINDEX('%[^0]%', @NORM_DB_PO  + '.'), 30);
        SET @NORM_SAP_PO = SUBSTRING(@NORM_SAP_PO, PATINDEX('%[^0]%', @NORM_SAP_PO + '.'), 30);
 
        IF (@NORM_DB_PO <> @NORM_SAP_PO)
            RAISERROR('SAP returned PO %s, which does not match QPS PO %s. Sync stopped.', 16, 1, @PO_NUMBER, @DB_PO_NUMBER);
 
        /* 3. Never overwrite with a zero or negative quantity (N2) */
        IF (ISNULL(@TOTAL_QTY, 0) <= 0)
            RAISERROR('SAP returned zero quantity for PO %s. Sync stopped and the existing details are retained.', 16, 1, @PO_NUMBER);
 
        /* 4. Quantity impact check - the sync is applied, but a warning is returned (E2, E3, E16, E17) */
        SELECT @OPEN_IR_QTY = ISNULL(SUM(OFFERED_QTY), 0)
        FROM INSPECTION_REQUESTS
        WHERE POID = @POID AND STATUS_CODE NOT IN (7, 9, 10);
 
        SELECT @PASSED_QTY = ISNULL(SUM(B.OFFERED_QTY), 0)
        FROM INSPECTION_REQUESTS A
        INNER JOIN INSPECTIONS B ON A.INSP_ID = B.INSPECTION_ID
        WHERE A.POID = @POID AND A.STATUS_CODE = 7 AND B.INSPECTION_RESULT = 1;
 
        SET @COMMITTED_QTY = @OPEN_IR_QTY + @PASSED_QTY;
        SET @NEW_LIMIT = CEILING(@TOTAL_QTY * 1.10);
 
        IF (@NEW_LIMIT < @COMMITTED_QTY)
            SET @WARNING_MESSAGE = CONCAT('WARNING: PO quantity changed in SAP from ', CAST(@OLD_TOTAL_QTY AS INT),
                ' to ', CAST(@TOTAL_QTY AS INT), '. Allowed quantity (110%) is ', @NEW_LIMIT, ' but ', @COMMITTED_QTY,
                ' is already offered or passed (open ', @OPEN_IR_QTY, ', passed ', @PASSED_QTY,
                '). Open requests must be reduced by ', @COMMITTED_QTY - @NEW_LIMIT, '.');
 
        /* 5. Audit values (same format as before, plus the new fields) */
        SELECT @LOG_OLD_VALUE =
              'PO_NUMBER = ' + ISNULL(CAST(PO_NUMBER AS VARCHAR(MAX)), '')
            + ', FABRIC = ' + ISNULL(CAST(FABRIC AS VARCHAR(MAX)), '')
            + ', COLOR = ' + ISNULL(CAST(COLOR AS VARCHAR(MAX)), '')
            + ', TOTAL_QTY = ' + ISNULL(CAST(TOTAL_QTY AS VARCHAR(MAX)), '')
            + ', APPARELS = ' + ISNULL(CAST(APPARELS AS VARCHAR(MAX)), '')
            + ', CATEGORY = ' + ISNULL(CAST(CATEGORY AS VARCHAR(MAX)), '')
            + ', MC = ' + ISNULL(CAST(MC AS VARCHAR(MAX)), '')
            + ', BUYER_ID = ' + ISNULL(CAST(BUYER_ID AS VARCHAR(MAX)), '')
            + ', MC_TEXT = ' + ISNULL(CAST(MC_TEXT AS VARCHAR(MAX)), '')
            + ', MRP = ' + ISNULL(CAST(MRP AS VARCHAR(MAX)), '')
            + ', BRAND_ID = ' + ISNULL(CAST(BRAND_ID AS VARCHAR(MAX)), '')
            + ', BRAND_ID2 = ' + ISNULL(CAST(BRAND_ID2 AS VARCHAR(MAX)), '')
            + ', FIT = ' + ISNULL(CAST(FIT AS VARCHAR(MAX)), '')
            + ', VENDOR_NAME = ' + ISNULL(CAST(VENDOR_NAME AS VARCHAR(MAX)), '')
            + ', BUYER_NAME = ' + ISNULL(CAST(BUYER_NAME AS VARCHAR(MAX)), '')
            + ', UPDATED_BY = ' + ISNULL(CAST((CASE WHEN ISNULL(UPDATED_BY, 0) = 0 THEN CREATED_BY ELSE UPDATED_BY END) AS VARCHAR(MAX)), '')
            + ', LAST_UPDATED_ON = ' + ISNULL(CONVERT(VARCHAR(19), LAST_UPDATED_ON, 120), '')
        FROM POHEADER WHERE POID = @POID;
 
        SET @LOG_NEW_VALUE =
              'PO_NUMBER = ' + ISNULL(CAST(@PO_NUMBER AS VARCHAR(MAX)), '')
            + ', FABRIC = ' + ISNULL(CAST(@FABRIC AS VARCHAR(MAX)), '')
            + ', COLOR = ' + ISNULL(CAST(@COLOR AS VARCHAR(MAX)), '')
            + ', TOTAL_QTY = ' + ISNULL(CAST(@TOTAL_QTY AS VARCHAR(MAX)), '')
            + ', APPARELS = ' + ISNULL(CAST(@APPARELS AS VARCHAR(MAX)), '')
            + ', CATEGORY = ' + ISNULL(CAST(@CATEGORY AS VARCHAR(MAX)), '')
            + ', MC = ' + ISNULL(CAST(@MC AS VARCHAR(MAX)), '')
            + ', BUYER_ID = ' + ISNULL(CAST(@BUYER_ID AS VARCHAR(MAX)), '')
            + ', MC_TEXT = ' + ISNULL(CAST(@MC_TEXT AS VARCHAR(MAX)), '')
            + ', MRP = ' + ISNULL(CAST(@MRP AS VARCHAR(MAX)), '')
            + ', BRAND_ID = ' + ISNULL(CAST(@BRAND_ID AS VARCHAR(MAX)), '')
            + ', BRAND_ID2 = ' + ISNULL(CAST(@BRAND_ID2 AS VARCHAR(MAX)), '')
            + ', FIT = ' + ISNULL(CAST(@FIT AS VARCHAR(MAX)), '')
            + ', VENDOR_NAME = ' + ISNULL(CAST(@VENDOR_NAME AS VARCHAR(MAX)), '')
            + ', BUYER_NAME = ' + ISNULL(CAST(@BUYER_NAME AS VARCHAR(MAX)), '')
            + ', UPDATED_BY = ' + ISNULL(CAST(@USER_ID AS VARCHAR(MAX)), '')
            + ', LAST_UPDATED_ON = ' + CONVERT(VARCHAR(19), GETDATE(), 120)
            + ISNULL(', ' + @WARNING_MESSAGE, '');
 
        /* 6. Apply the update */
        BEGIN TRANSACTION;
 
        UPDATE POHEADER SET
            FABRIC          = @FABRIC,
            COLOR           = @COLOR,
            TOTAL_QTY       = @TOTAL_QTY,
            APPARELS        = @APPARELS,
            CATEGORY        = @CATEGORY,
            MC              = @MC,
            BUYER_ID        = @BUYER_ID,
            MC_TEXT         = @MC_TEXT,
            MRP             = @MRP,
            BRAND_ID        = @BRAND_ID,
            BRAND_ID2       = @BRAND_ID2,
            FIT             = @FIT,
            VENDOR_NAME     = ISNULL(NULLIF(LTRIM(RTRIM(@VENDOR_NAME)), ''), VENDOR_NAME),
            BUYER_NAME      = ISNULL(NULLIF(LTRIM(RTRIM(@BUYER_NAME)), ''), BUYER_NAME),
            UPDATED_BY      = @USER_ID,
            LAST_UPDATED_ON = GETDATE()
        WHERE POID = @POID;
 
        IF (@@ROWCOUNT = 0)
            RAISERROR('PO %s was not updated. Please retry.', 16, 1, @PO_NUMBER);

        /* 7. Line items (N9): MERGE on (POID, LINE_ITEM) so PO_DETAILS_ID stays stable. Only when SAP lines were sent. */
        IF EXISTS (SELECT 1 FROM @PO_DETAILS)
        BEGIN
            IF EXISTS (SELECT 1 FROM @PO_DETAILS WHERE ISNULL(LTRIM(RTRIM(LINE_ITEM)), '') = '')
               OR EXISTS (SELECT LTRIM(RTRIM(LINE_ITEM)) FROM @PO_DETAILS GROUP BY LTRIM(RTRIM(LINE_ITEM)) HAVING COUNT(*) > 1)
            BEGIN
                -- the header is still updated; the lines are left as they are
                SET @WARNING_MESSAGE = CONCAT(@WARNING_MESSAGE + ' ',
                    'WARNING: SAP returned blank or repeated line item numbers, so the PO lines were not updated.');
            END
            ELSE
            BEGIN
                DECLARE @LINE_ACTIONS TABLE (ACT NVARCHAR(10));
                WITH T AS (SELECT * FROM PODETAIL WHERE POID = @POID)
                MERGE T
                USING (
                    SELECT LTRIM(RTRIM(LINE_ITEM)) AS LINE_ITEM, ISNULL(ARTICLE, '') AS ARTICLE, ARTICLE_DESC,
                           ISNULL(PREPACK_QTY, 0) AS PREPACK_QTY, ISNULL(PURCHASE_TOT_QTY, 0) AS PURCHASE_TOT_QTY,
                           ISNULL(PREPACK_QTY_EACHES, 0) AS PREPACK_QTY_EACHES, ISNULL(EACHES, 0) AS EACHES, MRP, ATTYP
                    FROM @PO_DETAILS
                ) AS S
                ON T.LINE_ITEM = S.LINE_ITEM
                WHEN MATCHED AND (
                       T.ARTICLE <> S.ARTICLE
                    OR ISNULL(T.ARTICLE_DESC, '') <> ISNULL(S.ARTICLE_DESC, '')
                    OR ISNULL(T.PREPACK_QTY, 0) <> S.PREPACK_QTY
                    OR ISNULL(T.PURCHASE_TOT_QTY, 0) <> S.PURCHASE_TOT_QTY
                    OR ISNULL(T.PREPACK_QTY_EACHES, 0) <> S.PREPACK_QTY_EACHES
                    OR ISNULL(T.EACHES, 0) <> S.EACHES
                    OR ISNULL(T.MRP, 0) <> ISNULL(S.MRP, 0)
                    OR ISNULL(T.ATTYP, '') <> ISNULL(S.ATTYP, '')) THEN
                    UPDATE SET ARTICLE = S.ARTICLE, ARTICLE_DESC = S.ARTICLE_DESC, PREPACK_QTY = S.PREPACK_QTY,
                               PURCHASE_TOT_QTY = S.PURCHASE_TOT_QTY, PREPACK_QTY_EACHES = S.PREPACK_QTY_EACHES,
                               EACHES = S.EACHES, MRP = S.MRP, ATTYP = S.ATTYP,
                               UPDATED_BY = @USER_ID, LAST_UPDATED_ON = GETDATE()
                WHEN NOT MATCHED BY TARGET THEN
                    INSERT (POID, LINE_ITEM, ARTICLE, ARTICLE_DESC, PREPACK_QTY, PURCHASE_TOT_QTY, PREPACK_QTY_EACHES, EACHES, MRP, ATTYP, CREATED_ON, CREATED_BY)
                    VALUES (@POID, S.LINE_ITEM, S.ARTICLE, S.ARTICLE_DESC, S.PREPACK_QTY, S.PURCHASE_TOT_QTY, S.PREPACK_QTY_EACHES, S.EACHES, S.MRP, S.ATTYP, GETDATE(), @USER_ID)
                WHEN NOT MATCHED BY SOURCE THEN
                    DELETE    -- line removed from the PO in SAP
                OUTPUT $action INTO @LINE_ACTIONS (ACT);

                IF EXISTS (SELECT 1 FROM @LINE_ACTIONS)
                    SET @LOG_NEW_VALUE = CONCAT(@LOG_NEW_VALUE,
                        ', PODETAIL: ', (SELECT COUNT(*) FROM @LINE_ACTIONS WHERE ACT = 'UPDATE'), ' updated, ',
                        (SELECT COUNT(*) FROM @LINE_ACTIONS WHERE ACT = 'INSERT'), ' added, ',
                        (SELECT COUNT(*) FROM @LINE_ACTIONS WHERE ACT = 'DELETE'), ' removed');
            END
        END
 
        INSERT INTO [dbo].[SPTRACE]([SP_OLDVALUES], [SP_NEWVALUES]) VALUES (@LOG_OLD_VALUE, @LOG_NEW_VALUE);
 
        SET @RECORD_ID = @POID;
        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0
            ROLLBACK TRANSACTION;
 
        SET @ERROR_CODE = ERROR_NUMBER();
        SET @ERROR_MESSAGE = ERROR_MESSAGE();
        SET @WARNING_MESSAGE = NULL;
    END CATCH
END
GO

SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE OR ALTER PROCEDURE [dbo].[SP_SAVE_PO]
(
    @POID INT OUTPUT,                    -- if 0 -> INSERT, else UPDATE
    @PO_NUMBER VARCHAR(30),
    @VENDOR_CODE VARCHAR(30),
    @VENDOR_NAME VARCHAR(100),
    @FABRIC VARCHAR(255),
    @COLOR VARCHAR(100),
    @TOTAL_QTY DECIMAL(18,3),
	@APPARELS VARCHAR(50)= NULL,
	@CATEGORY VARCHAR(50)= NULL,
	@BUYER_NAME VARCHAR(50)= NULL,
	@MC VARCHAR(50)= NULL,
	@MC_TEXT VARCHAR(250)= NULL,
	@MRP DECIMAL(18,3)=NULL,
	@BUYER_ID VARCHAR(20)= NULL,
	@BRAND_ID VARCHAR(250)= NULL,
	@BRAND_ID2 VARCHAR(250)= NULL,
	@FIT VARCHAR(250)= NULL,
    @USER_ID INT,                        -- created/updated by
    @IS_CANCELLED BIT = 0,  
    -- Inspection Request (optional)
	@INSP_ID BIGINT = NULL,
    @INSPECTION_DATE DATETIME = NULL,
    @OFFERED_QTY DECIMAL(18,3) = NULL,
    @ADDRESS_ID INT = NULL,
	@REGION_ID INT= NULL,

	@INSPECTION_TYPE_ID INT= NULL,
	
	@IS_QC_AT_DC BIT = 0,
    -- TABLE TYPE
    @DETAILS TVP_PODETAIL READONLY,
	@RECORD_ID INT OUTPUT,  
	@ERROR_CODE INT = 0 OUTPUT,             
    @ERROR_MESSAGE VARCHAR(500) = 'Success' OUTPUT
)
AS
BEGIN
    SET NOCOUNT ON;
	DECLARE @AvailableQty int,
	@avlqty Varchar(20),
    @Now DATETIME,
    @MinAllowedDT DATETIME,
	@InitialTarget DATETIME,
    @SundayCount INT;
	
    BEGIN TRY
		SET @ERROR_CODE = 0;
        SET @ERROR_MESSAGE = 'Success';
		SET @RECORD_ID = 0;
        BEGIN TRANSACTION;

		IF(@IS_CANCELLED=1)
		BEGIN
			UPDATE INSPECTION_REQUESTS						
			SET 							
				LAST_UPDATED_ON = GETDATE(),
				UPDATED_BY = @USER_ID,
				STATUS_CODE= case when @IS_CANCELLED = 1 then 9 else 2 end
			WHERE INSP_ID = @INSP_ID;
			SET @RECORD_ID = @INSP_ID;
		END
		ELSE
		BEGIN

		-- FIX 1: NULL means "new". Treat NULL as 0 so every "= 0" and "!= @INSP_ID" check below works
		-- (with NULL, "!=" is never true, so the quantity sums ignored every row and the checks were skipped).
		SET @INSP_ID = ISNULL(@INSP_ID, 0);
		SET @POID    = ISNULL(@POID, 0);

		-- N11: one quantity-changing save per PO at a time (same lock as SP_SAVE_INSPECTION). The transaction is
		-- already open, so the quantity validation below runs under the lock. A PO that is new to QPS is locked
		-- by its number, so two vendors cannot create it twice at the same moment.
		DECLARE @LOCK_RESOURCE NVARCHAR(255) =
			IIF(@POID > 0, CONCAT(N'QPS_PO_QTY_', @POID), CONCAT(N'QPS_PO_NEW_', LTRIM(RTRIM(@PO_NUMBER)))),
			@LOCK_RESULT INT;
		EXEC @LOCK_RESULT = sp_getapplock @Resource = @LOCK_RESOURCE, @LockMode = 'Exclusive', @LockOwner = 'Transaction', @LockTimeout = 10000;
		IF (@LOCK_RESULT < 0)
			RAISERROR('Another request for PO %s is being saved. Please try again in a moment.', 16, 1, @PO_NUMBER);

		-- CHANGE 1: existing PO -> validate against the quantity stored in QPS (last SAP sync),
		-- never the value sent by the screen (E5, N10).
		IF (@POID > 0)
		BEGIN
			SELECT @TOTAL_QTY = TOTAL_QTY FROM POHEADER WHERE POID = @POID;

			-- FIX 4: an unknown POID must not fall back to the screen's quantity.
			IF (@@ROWCOUNT = 0)
			BEGIN
				RAISERROR('PO %s (POID %d) was not found in QPS. Refresh the PO from SAP and try again.', 16, 1, @PO_NUMBER, @POID);
			END
		END

		IF(@INSP_ID=0)
		BEGIN
			IF EXISTS (SELECT 1 FROM INSPECTION_REQUESTS WHERE POID = @POID AND STATUS_CODE NOT IN (7,9,10))
			BEGIN
				-- FIX 2: no RETURN after RAISERROR (severity 16 inside TRY jumps straight to CATCH).
				RAISERROR('Last Inspection request is under process. PO: %s', 16, 1,@PO_NUMBER);
		   
			END
		END
		
		IF(@IS_QC_AT_DC=0)
			BEGIN
				IF(@INSP_ID=0)
					BEGIN
						SET @Now  = GETDATE();
					END
				ELSE
					BEGIN
						SELECT @Now = CREATED_ON FROM INSPECTION_REQUESTS WHERE INSP_ID = @INSP_ID;
					END
					-- Initial 48-hour target
				SET @InitialTarget  = DATEADD(HOUR, 48, @Now);

				-- Count number of Sundays crossed
				SET @SundayCount =
				(
					SELECT COUNT(*)
					FROM
					(
						SELECT TOP (DATEDIFF(DAY, @Now, @InitialTarget) + 1)
								DATEADD(DAY, ROW_NUMBER() OVER (ORDER BY (SELECT NULL)) - 1, CAST(@Now AS DATE)) AS D
						FROM master..spt_values
					) X
					WHERE DATENAME(WEEKDAY, D) = 'Sunday'
				);

				SET @MinAllowedDT = DATEADD(HOUR, 48 + (@SundayCount * 24), @Now);

				DECLARE @InspIdStr VARCHAR(500);
				SET @InspIdStr ='MINIMUM ALLOWD : '+ CONVERT(VARCHAR(50), @MinAllowedDT)+ ',  INSERTED : ' + CONVERT(VARCHAR(50), @INSPECTION_DATE);
				IF @INSPECTION_DATE IS NOT NULL   AND @INSPECTION_DATE < @MinAllowedDT
					BEGIN
						RAISERROR (
							'Please select a Date of Inspection that is at least 48 working hours  later (Sundays excluded).  %s.',
							16,
							1,
							 @InspIdStr
						);
					END
			END
		IF(@IS_QC_AT_DC=1)
			BEGIN
				SET @REGION_ID=1;-- All QC AT DC Request will go to Ram Prakash Soni Ji.
			END
		-----------------------------------------
		-- 1. INSERT or UPDATE POHEADER
		-----------------------------------------

		 -- Change here to apply 10% exceed logic 
	  
			IF (@OFFERED_QTY > CEILING(@TOTAL_QTY * 1.10)) --(@TOTAL_QTY<@OFFERED_QTY)
				BEGIN
					RAISERROR (
						'Offered quantity exceeds the allowed 10 percent additional limit of the total quantity.',
						16,
						1,
						 ''
					);
				END
		
			IF(@POID = 0)
			BEGIN
			   IF EXISTS (SELECT 1 FROM POHEADER WHERE PO_NUMBER = @PO_NUMBER)
                 BEGIN
                     -- FIX 2: no RETURN after RAISERROR.
                     RAISERROR('PO_NUMBER %s already exists.', 16, 1, @PO_NUMBER);
							
                 END
				INSERT INTO POHEADER
				(
					PO_NUMBER, VENDOR_CODE, VENDOR_NAME, FABRIC, COLOR,TOTAL_QTY, APPARELS,CATEGORY,BUYER_NAME,BUYER_ID,MC,MC_TEXT,MRP,BRAND_ID,BRAND_ID2,FIT, CREATED_ON, CREATED_BY
				)
				VALUES
				(
					@PO_NUMBER, @VENDOR_CODE, @VENDOR_NAME, @FABRIC, @COLOR, @TOTAL_QTY, @APPARELS, @CATEGORY, @BUYER_NAME, @BUYER_ID,@MC,@MC_TEXT,@MRP, @BRAND_ID,@BRAND_ID2,@FIT,GETDATE(), @USER_ID
				);

				SET @POID = SCOPE_IDENTITY();

					-----------------------------------------
					-- 2. UPSERT PODETAIL using MERGE
					-----------------------------------------
					MERGE PODETAIL AS Target
					USING (
						SELECT 
							PO_DETAILS_ID,
							LINE_ITEM, ARTICLE, ARTICLE_DESC, 
							PREPACK_QTY, PURCHASE_TOT_QTY,
							PREPACK_QTY_EACHES, EACHES,MRP,ATTYP,
							CREATED_BY, UPDATED_BY
						FROM @DETAILS
					) AS Source
					ON Target.PO_DETAILS_ID = Source.PO_DETAILS_ID  AND Target.POID = @POID

					WHEN MATCHED THEN 
						UPDATE SET
							LINE_ITEM = Source.LINE_ITEM,
							ARTICLE = Source.ARTICLE,
							ARTICLE_DESC = Source.ARTICLE_DESC,
							PREPACK_QTY = Source.PREPACK_QTY,
							PURCHASE_TOT_QTY = Source.PURCHASE_TOT_QTY,
							PREPACK_QTY_EACHES = Source.PREPACK_QTY_EACHES,
							EACHES = Source.EACHES,
							MRP = Source.MRP,
							ATTYP = Source.ATTYP,
							LAST_UPDATED_ON = GETDATE(),
							UPDATED_BY = Source.UPDATED_BY

					WHEN NOT MATCHED BY TARGET THEN
						INSERT 
						(
							POID, LINE_ITEM, ARTICLE, ARTICLE_DESC,
							PREPACK_QTY, PURCHASE_TOT_QTY, 
							PREPACK_QTY_EACHES, EACHES,MRP,ATTYP,
							CREATED_ON, CREATED_BY
						)
						VALUES
						(
							@POID, Source.LINE_ITEM, Source.ARTICLE, Source.ARTICLE_DESC,
							Source.PREPACK_QTY, Source.PURCHASE_TOT_QTY,
							Source.PREPACK_QTY_EACHES, Source.EACHES,Source.MRP,Source.ATTYP,
							GETDATE(), Source.CREATED_BY
						)

					WHEN NOT MATCHED BY SOURCE AND Target.POID = @POID THEN
						DELETE;     -- remove deleted rows
				-----------------------------------------

			END
			-----------------------------------------
			--Offered quantity exceeds the allowed 10% additional limit of the total quantity.
			-- 3. UPSERT INSPECTION REQUEST
			-- Validation Logic
			-- 1. Fetch all inspection requests for the same PO where inspection is not completed (STATUS_CODE <> 7), excluding the current inspection request.
			-- 2. Fetch only completed inspection requests (STATUS_CODE = 7) where the inspection result is PASS (INSPECTION_RESULT = 1).
			-- 3. Calculate Available Quantity = Total PO Quantity – Sum of Offered Quantities.
			-- 4. Validate that the current Offered Quantity does not exceed 110% of the Available Quantity.
			-----------------------------------------
			-- FIX 3: same type as the quantities (DECIMAL(18,3)), so 10.5 is no longer cut to 10.
			DECLARE @IROFFEREDQTY DECIMAL(18,3), @QAOFFEREDQTY DECIMAL(18,3), @AVAILABLE_QTY DECIMAL(18,3)
			-- 1. Fetch all inspection requests for the same PO where inspection is not completed (STATUS_CODE <> 7), excluding the current inspection request.
			SELECT @IROFFEREDQTY= ISNULL(SUM(A.OFFERED_QTY),0) FROM INSPECTION_REQUESTS A 
			LEFT JOIN POHEADER C on A.POID=C.POID 
			WHERE C.POID=@POID AND STATUS_CODE NOT IN (7,9,10) AND A.INSP_ID !=@INSP_ID

			-- 2. Fetch only completed inspection requests (STATUS_CODE = 7) where the inspection result is PASS (INSPECTION_RESULT = 1).
			-- 3. Calculate Available Quantity = Total PO Quantity – Sum of Offered Quantities.
			SELECT @AVAILABLE_QTY =
				(
				(CEILING(@TOTAL_QTY * 1.10)) - (@IROFFEREDQTY+ISNULL(SUM(B.OFFERED_QTY),0))
				)
			FROM INSPECTION_REQUESTS A 
			LEFT JOIN INSPECTIONS B on A.INSP_ID=B.INSPECTION_ID 
			LEFT JOIN POHEADER C on A.POID=C.POID 
			WHERE C.POID=@POID AND STATUS_CODE=7  AND B.INSPECTION_RESULT=1 AND A.INSP_ID !=@INSP_ID
	
			-- 4. Validate that the current Offered Quantity does not exceed 110% of the Available Quantity.
	   
			-- FIX 3: show 120 or 10.5, not 120.000 or 10.500.
			SET @avlqty = FORMAT(@AVAILABLE_QTY, '0.###')
			IF(@AVAILABLE_QTY >0 )
				BEGIN
					IF(@OFFERED_QTY > @AVAILABLE_QTY) -- Check the 10% logic here
						BEGIN
							RAISERROR (
								'Offered quantity exceeds the permitted 10 percent tolerance above the total quantity. Available quantity: %s.',
								16,
								1,
								@avlqty 
							);
						END
				END
			ELSE
				BEGIN
					-- CHANGE 2 (E3): no confusing negative "available" figure.
					SET @avlqty = FORMAT(IIF(@AVAILABLE_QTY < 0, 0, ISNULL(@AVAILABLE_QTY, 0)), '0.###');
					RAISERROR (
						'PO %s quantity is fully consumed (available quantity: %s). No further inspection request can be raised.',
						16,
						1,
						@PO_NUMBER,
						@avlqty
					);
				END
	   
	   
			INSERT INTO [dbo].[IROFRQTYLOGICCHECK]([PONumber],[IRId],[OfferedQty],[TotalQty],[FirstCheck],[IROfferedQty],[QAOfferedQty],[AvailableQty],[AvgQty],[SecondCheck])
			VALUES (@PO_NUMBER, @INSP_ID, @OFFERED_QTY , @TOTAL_QTY, (IIF(@OFFERED_QTY > CEILING(@TOTAL_QTY * 1.10), 1, 0)) ,@IROFFEREDQTY, @QAOFFEREDQTY, @AVAILABLE_QTY, @avlqty,(IIF(@OFFERED_QTY > CEILING(@AVAILABLE_QTY * 1.10), 1, 0)));

			IF(@INSPECTION_DATE IS NOT NULL AND @OFFERED_QTY > 0 AND @ADDRESS_ID IS NOT NULL)
				BEGIN
					IF(@INSP_ID = 0)
						BEGIN
							INSERT INTO INSPECTION_REQUESTS
							(
								POID, OFFERED_QTY, DATE_OF_INSPECTION, ADDRESS_ID, REGION_ID,STATUS_CODE, 
								INSPECTION_TYPE_ID, IS_QC_AT_DC, CREATED_ON, CREATED_BY, LAST_UPDATED_ON, UPDATED_BY
							)
							VALUES
							(
								@POID, @OFFERED_QTY, @INSPECTION_DATE, @ADDRESS_ID, @REGION_ID,1, 
								@INSPECTION_TYPE_ID, @IS_QC_AT_DC, GETDATE(), @USER_ID, GETDATE(), @USER_ID
							);
							SET @RECORD_ID = SCOPE_IDENTITY();
						END
					ELSE
						BEGIN
							UPDATE INSPECTION_REQUESTS
							SET 
								OFFERED_QTY = @OFFERED_QTY,
								DATE_OF_INSPECTION = @INSPECTION_DATE,
								ADDRESS_ID = @ADDRESS_ID,
								REGION_ID = @REGION_ID,
								INSPECTION_TYPE_ID = @INSPECTION_TYPE_ID, 
								IS_QC_AT_DC=@IS_QC_AT_DC,
								LAST_UPDATED_ON = GETDATE(),
								UPDATED_BY = @USER_ID,
								STATUS_CODE= case when @IS_CANCELLED = 1 then 9 else 2 end
							WHERE INSP_ID = @INSP_ID;
							SET @RECORD_ID = @INSP_ID;
						END
			END
			ELSE
				BEGIN
					RAISERROR (
						'Date of Inspection & Offered Qty and Address field is required.',
						16,
						1,
						 ''
					);
				END
		END
        COMMIT TRANSACTION;
		
    END TRY

    BEGIN CATCH
		IF @@TRANCOUNT > 0
            ROLLBACK TRANSACTION;
        SET @ERROR_CODE = ERROR_NUMBER();
        SET @ERROR_MESSAGE = ERROR_MESSAGE();
    END CATCH
END
GO

SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE OR ALTER PROCEDURE [dbo].[SP_SAVE_INSPECTION]
(
    @INSPECT_ID BIGINT = 0,              -- 0 = INSERT, >0 = UPDATE
    @INSPECTION_ID BIGINT,
    @INSPECTOR_CODE VARCHAR(50),
	@OFFERED_QTY INT=0,
	@SHIP_QTY INT=0,
    @GPT_NO VARCHAR(50),
    @GPT_STATUS INT,
    @GPT_APPROVED_BY VARCHAR(150) = NULL,
    @GPT_APPROVED_DATE DATE = NULL,

	@IS_RESET_BY_ADMIN BIT = 0,

    @NO_OF_BAGS_CARTONS_QTY INT,
    @NO_OF_BAGS_CARTONS_UNIT INT,
    @PACKING_STATUS INT = NULL,
    @INLINE_DONE INT,
    @PACKING_LIST INT,

    @PPS_RELEASE_DATE DATE = NULL,
    @PPS_EXPIRY_DATE DATE = NULL,

    @FIBER_CONTENT_ON_PO VARCHAR(250),
    @FIBER_CONTENT_ON_TEST_REPORT VARCHAR(250),
    @FIBER_CONTENT_ON_WC VARCHAR(250),

    @GSM_ON_PO VARCHAR(150),
    @GSM_ON_TEST_REPORT VARCHAR(150),
    @GSM_ON_BULK VARCHAR(150) = NULL,

    @MEASUREMENT_SAMPLE_SIZE INT,
    @MEASUREMENT_ATTACHMENT VARCHAR(150) = NULL,

    @NO_OF_CHECK INT,
    @NO_OF_CHECK_UNIT INT,
    @NO_OF_PREPACK_CHECK INT = NULL,

    @WORKMANSHIP VARCHAR(50) = NULL,
    @INSPECTION_RESULT INT,

    @REMARK_COMMENT VARCHAR(500) = NULL,
    @NOTE VARCHAR(500) = NULL,
    @NAME_OF_FACTORY_REPRESENTATIVE VARCHAR(150),

    @TEST_REPORT_ATTACHMENT VARCHAR(1000) = NULL,  
    @GEN_ATTACHMENT_FIRST VARCHAR(1000) = NULL,
    @GEN_ATTACHMENT_SECOND VARCHAR(150) = NULL,

    @IS_COMPLETE BIT,
    @USER_ID INT,

	@RESEAN_FOR_FAIL INT,
	@COLOR_CODE INT,
	@MFG_DATE DATE = NULL,
	@GPT_COMM_APPROVAL_DATE DATE = NULL,
	@DATE_OF_COMPLETION DATETIME = NULL,
	@SEASON VARCHAR(50) = NULL,
	@AREA_OF_FAIL_GPTR VARCHAR(50) = NULL,
	@GPT_COMM_APPROVED_BY VARCHAR(50) = NULL,
	@IS_EDIT_BY_ADMIN BIT,
	@GPT_ELASTIC_NO varchar(50)	,
    @GPT_ELASTIC_STATUS int,
    @GPT_ELASTIC_APPROVED_BY varchar(150),
    @GPT_ELASTIC_APPROVED_DATE date,
    @CONFORMITY_SIZE varchar(150),
    @CONFORMITY_SIZE_STATUS int,
    @CONFORMITY_PREPACK varchar(150),
    @CONFORMITY_PREPACK_STATUS int,
    @EMBROIDERY_PF int,
    @ACCESSORIES_PF int,
    @BARCODE_PF int,
    @NO_PLASTIC_PF int,
    @BOGO_PF int,
    @THREAD_YN int,
    @BUTTON_YN int,
    @ZIPPER_YN int,
    @INTERLINING_YN int,
    @ELASTIC_YN int,
    @MEASUREMENT_SAMPLE_QTY int,

	@ZIPPER_RPT_NO VARCHAR(500)= NULL,
	@ZIPPER_STATUS INT= NULL,
	@ZIPPER_APPROVED_BY VARCHAR(150)= NULL,
	@ZIPPER_APPROVED_DATE DATE= NULL,
	@FIT VARCHAR(150)= NULL,
	@COMM_APPROVAL_DOC VARCHAR(500)= NULL,
	@DEBIT_AMOUNT DECIMAL(12,2)= NULL,
	@ADMIN_REMARK VARCHAR(250)= NULL,
	@ABORTIVE_REASON_IDS VARCHAR(250)= NULL,
	@OTHER_ABORTIVE_REASON VARCHAR(MAX)=NULL,
	@AMT_TRAVELLING int,
	@SELECTED_BAG_CARTONS_NO VARCHAR(100),

    @DEFECTS DBO.TVP_INSPECTION_DEFECTS READONLY,

    -- OUTPUT PARAMETERS
	@OUT_INSPECT_ID BIGINT=0 OUTPUT,  
	@ERROR_CODE INT OUTPUT,             
    @ERROR_MESSAGE VARCHAR(500)  OUTPUT
)
AS
BEGIN
    SET NOCOUNT ON;

    BEGIN TRY
		
		SET @OUT_INSPECT_ID = @INSPECT_ID;
		SET @ERROR_CODE = 0;
		SET @ERROR_MESSAGE = 'Success';

		        /* ===== REPLACES: from "DECLARE @PONUMBER ..." down to (not including) "BEGIN TRAN;" ===== */
 
        DECLARE @PONUMBER VARCHAR(30), @AQL_ID INT, @DEFECTS_STATUS INT, @AQL2_5_SAMPLE_SIZE INT,
                @DEFECTS_ALLOWED_MAJOR INT = 0, @DEFECTS_ALLOWED_MINOR INT = 0,
                @TOTAL_QTY DECIMAL(18,3), @POID INT, @IsAllreadyComplete BIT,
                @LAST_UPDATE_DATE_BY_ADMIN DATETIME = NULL;
 
        DECLARE @QA_EMP_CODE_IN_REQUEST VARCHAR(20), @EMP_CODE_IN_REPORT VARCHAR(20),
                @INSP_ID BIGINT = @INSPECTION_ID, @ROLEID INT;
 
        DECLARE @IROFFEREDQTY INT = 0, @PASSED_QTY INT = 0, @QAOFFEREDQTY INT, @CIROFFEREDQTY INT,
                @STORED_SHIP_QTY INT = NULL, @QTY_LIMIT INT = 0, @AVAILABLE_QTY INT = 0,
                @DO_QTY_CHECK BIT = 0;
 
        SELECT @ROLEID = ROLE_ID, @EMP_CODE_IN_REPORT = EMP_CODE
        FROM USER_MASTER WHERE ID = @USER_ID;
 
        SELECT @IsAllreadyComplete = IS_COMPLETE,
               @QA_EMP_CODE_IN_REQUEST = INSPECTOR_CODE,
               @STORED_SHIP_QTY = OFFERED_QTY
        FROM INSPECTIONS WHERE INSPECT_ID = @INSPECT_ID;
 
        SELECT @CIROFFEREDQTY = OFFERED_QTY, @POID = POID
        FROM INSPECTION_REQUESTS WHERE INSP_ID = @INSPECTION_ID;
        /* Only QA Admin (1008) may re-open a completed report (PO Sync Case Analysis, open point 7) */
        IF (@IS_RESET_BY_ADMIN = 1 AND ISNULL(@ROLEID, 0) <> 1008)
        BEGIN
            SET @ERROR_CODE = 50005;
            SET @ERROR_MESSAGE = 'Only QA Admin can reset a completed inspection.';
            RETURN;
        END
        /* N11: one quantity-changing save per PO at a time. The transaction and the PO lock start BEFORE the
           quantity validation, so two saves for the same PO cannot both pass it. SP_SAVE_PO takes the same lock. */
        DECLARE @LOCK_RESOURCE NVARCHAR(255) = CONCAT(N'QPS_PO_QTY_', ISNULL(@POID, 0)), @LOCK_RESULT INT;
        BEGIN TRAN;
        EXEC @LOCK_RESULT = sp_getapplock @Resource = @LOCK_RESOURCE, @LockMode = 'Exclusive', @LockOwner = 'Transaction', @LockTimeout = 10000;
        IF (@LOCK_RESULT < 0)
        BEGIN
            ROLLBACK TRAN;
            SET @ERROR_CODE = 50006;
            SET @ERROR_MESSAGE = 'Another save for this PO is in progress. Please try again in a moment.';
            RETURN;
        END
        /* E12: PO values as they are when the report is completed (first completion, or completion after a reset) */
        DECLARE @TAKE_SNAPSHOT BIT = IIF(@IS_RESET_BY_ADMIN = 0 AND @IS_COMPLETE = 1 AND ISNULL(@IsAllreadyComplete, 0) = 0, 1, 0),
                @SNAP_QTY DECIMAL(18,3), @SNAP_FABRIC VARCHAR(255), @SNAP_COLOR VARCHAR(100), @SNAP_MRP DECIMAL(18,2);
        SELECT @SNAP_QTY = TOTAL_QTY, @SNAP_FABRIC = FABRIC, @SNAP_COLOR = COLOR, @SNAP_MRP = MRP FROM POHEADER WHERE POID = @POID;
 
        /* Admin reset only re-opens the report, so no data validation is needed (E4, E17) */
        IF (@IS_RESET_BY_ADMIN = 0)
        BEGIN
            IF (@SHIP_QTY < 1 AND @IS_COMPLETE = 1)
            BEGIN
                SET @ERROR_CODE = 50000;
                SET @ERROR_MESSAGE = 'Ship quantity is zero.';
                ROLLBACK TRAN;
                RETURN;
            END
 
            IF (@INSPECTION_RESULT = 4 AND @AMT_TRAVELLING = 0)
            BEGIN
                SET @ERROR_CODE = 50001;
                SET @ERROR_MESSAGE = 'Please enter the travelling amount.';
                ROLLBACK TRAN;
                RETURN;
            END
 
            IF (@IS_COMPLETE = 1 AND @ROLEID != 1008 AND @QA_EMP_CODE_IN_REQUEST = @EMP_CODE_IN_REPORT
                AND (@TEST_REPORT_ATTACHMENT IS NULL OR LTRIM(RTRIM(@TEST_REPORT_ATTACHMENT)) = ''))
            BEGIN
                SET @ERROR_CODE = 50002;
                SET @ERROR_MESSAGE = 'Attachment is required.';
                ROLLBACK TRAN;
                RETURN;
            END
 
            /* ---------- Quantity validation ---------- */
            SELECT @TOTAL_QTY = TOTAL_QTY, @PONUMBER = PO_NUMBER FROM POHEADER WHERE POID = @POID;
            SET @QTY_LIMIT = CEILING(ISNULL(@TOTAL_QTY, 0) * 1.10);
 
            -- Open requests of the same PO (not completed / cancelled / abortive), excluding this one
            SELECT @IROFFEREDQTY = ISNULL(SUM(OFFERED_QTY), 0)
            FROM INSPECTION_REQUESTS
            WHERE POID = @POID AND STATUS_CODE NOT IN (7, 9, 10) AND INSP_ID <> @INSP_ID;
 
            -- Ship quantity already PASSED on completed requests, excluding this one
            SELECT @PASSED_QTY = ISNULL(SUM(B.OFFERED_QTY), 0)
            FROM INSPECTION_REQUESTS A
            INNER JOIN INSPECTIONS B ON A.INSP_ID = B.INSPECTION_ID
            WHERE A.POID = @POID AND A.STATUS_CODE = 7 AND B.INSPECTION_RESULT = 1 AND A.INSP_ID <> @INSP_ID;
 
            SET @AVAILABLE_QTY = @QTY_LIMIT - (@IROFFEREDQTY + @PASSED_QTY);
            SET @QAOFFEREDQTY = @SHIP_QTY;
 
            -- Validate only when quantities are being changed, or on the first submission (E2, E4, E16, E17)
            IF (ISNULL(@STORED_SHIP_QTY, -1) <> @SHIP_QTY
                OR ISNULL(@CIROFFEREDQTY, -1) <> @OFFERED_QTY
                OR (@IS_COMPLETE = 1 AND ISNULL(@IsAllreadyComplete, 0) = 0))
                SET @DO_QTY_CHECK = 1;
 
            IF (@DO_QTY_CHECK = 1 AND (@SHIP_QTY > @AVAILABLE_QTY OR @OFFERED_QTY > @AVAILABLE_QTY))
            BEGIN
                SET @ERROR_CODE = 50001;
                SET @ERROR_MESSAGE =
                    CASE
                        WHEN @AVAILABLE_QTY <= 0 THEN
                            CONCAT('PO ', @PONUMBER, ' quantity is fully consumed. PO qty ', CAST(@TOTAL_QTY AS INT),
                                   ' (+10% = ', @QTY_LIMIT, '), already passed ', @PASSED_QTY,
                                   ', other open requests ', @IROFFEREDQTY, '.')
                        WHEN @SHIP_QTY > @AVAILABLE_QTY THEN
                            CONCAT('Ship quantity (', @SHIP_QTY, ') exceeds the available quantity (', @AVAILABLE_QTY,
                                   ') for PO ', @PONUMBER, '. PO qty ', CAST(@TOTAL_QTY AS INT), ' +10% = ', @QTY_LIMIT,
                                   ', already passed ', @PASSED_QTY, ', other open requests ', @IROFFEREDQTY, '.')
                        ELSE
                            CONCAT('Offered quantity (', @OFFERED_QTY, ') exceeds the available quantity (', @AVAILABLE_QTY,
                                   ') for PO ', @PONUMBER, '. PO qty ', CAST(@TOTAL_QTY AS INT), ' +10% = ', @QTY_LIMIT,
                                   ', already passed ', @PASSED_QTY, ', other open requests ', @IROFFEREDQTY, '.')
                    END;
                ROLLBACK TRAN;
                RETURN;
            END
 
            IF (@IS_COMPLETE = 1)
            BEGIN
                INSERT INTO [dbo].[IROFRQTYLOGICCHECK]
                    ([PONumber],[IRId],[InspId],[OfferedQty],[TotalQty],[FirstCheck],[IROfferedQty],[QAOfferedQty],[AvailableQty],[SecondCheck])
                VALUES
                    (@PONUMBER, @INSP_ID, @INSPECT_ID, @CIROFFEREDQTY, @TOTAL_QTY,
                     IIF(@SHIP_QTY > CEILING(@TOTAL_QTY * 1.10), 1, 0),
                     @IROFFEREDQTY, @QAOFFEREDQTY, @AVAILABLE_QTY,
                     IIF(@SHIP_QTY > CEILING(@AVAILABLE_QTY * 1.10), 1, 0));
            END
 
            /* ---------- AQL, PPS and defects (unchanged) ---------- */
            SELECT @AQL_ID = AQL_ID, @AQL2_5_SAMPLE_SIZE = SAMPLE_SIZE,
                   @DEFECTS_ALLOWED_MAJOR = AQL_2_5_MAJOR, @DEFECTS_ALLOWED_MINOR = AQL_2_5_MINOR
            FROM [dbo].[MASTER_AQL]
            WHERE @SHIP_QTY BETWEEN BATCH_SIZE_MIN AND BATCH_SIZE_MAX;
 
            IF @PPS_RELEASE_DATE IS NOT NULL AND @PPS_EXPIRY_DATE IS NOT NULL
               AND @PPS_EXPIRY_DATE < DATEADD(DAY, -1, DATEADD(MONTH, 6, @PPS_RELEASE_DATE))
            BEGIN
                SET @ERROR_CODE = 50001;
                SET @ERROR_MESSAGE = 'PPS Expiry Date must be 6 months greater than Release Date';
                ROLLBACK TRAN;
                RETURN;
            END
 
            IF (SELECT SUM(ISNULL(MAJOR, 0)) FROM @DEFECTS) > @DEFECTS_ALLOWED_MAJOR
               OR (SELECT SUM(ISNULL(MINOR, 0)) FROM @DEFECTS) > @DEFECTS_ALLOWED_MINOR
                SET @DEFECTS_STATUS = 2;
            ELSE
                SET @DEFECTS_STATUS = 1;
 
            IF @INSPECTION_RESULT = 2 AND NOT EXISTS (SELECT 1 FROM @DEFECTS)
            BEGIN
                SET @ERROR_CODE = 50003;
                SET @ERROR_MESSAGE = 'Defects are mandatory when inspection result is FAIL';
                ROLLBACK TRAN;
                RETURN;
            END
        END
 
        /* ===== END OF REPLACED BLOCK - the transaction is already open (see N11 above) ===== */

		IF(@IS_RESET_BY_ADMIN=1)
			BEGIN
				UPDATE dbo.INSPECTIONS SET DATE_OF_COMPLETION=NULL,IS_COMPLETE = 0, TEST_REPORT_ATTACHMENT = NULL, LAST_UPDATE_DATE_BY_ADMIN=NULL, GEN_ATTACHMENT_FIRST = NULL, MODIFIED_DATE = GETDATE(),MODIFIED_BY = @USER_ID WHERE INSPECT_ID = @INSPECT_ID;
				UPDATE INSPECTION_REQUESTS	SET STATUS_CODE=8, LAST_UPDATED_ON = GETDATE(), UPDATED_BY = @USER_ID WHERE INSP_ID = @INSPECTION_ID;
			END
		ELSE
			BEGIN

				 /* NEW position: the IR offered quantity is now updated INSIDE the transaction,
                   so it rolls back if any later step fails (N12) */
                IF (@OFFERED_QTY <> @CIROFFEREDQTY)
                BEGIN
                    UPDATE INSPECTION_REQUESTS  SET OFFERED_QTY = @OFFERED_QTY, LAST_UPDATED_ON = GETDATE(), UPDATED_BY = @USER_ID WHERE INSP_ID = @INSPECTION_ID;
                END
				/* =============================
				    INSERT / UPDATE INSPECTION
				 ==============================*/
				IF ISNULL(@INSPECT_ID, 0) = 0
					BEGIN
						INSERT INTO dbo.INSPECTIONS
						(
						    INSPECTION_ID, INSPECTOR_CODE,OFFERED_QTY,AQL_ID, GPT_NO, GPT_STATUS,
						    GPT_APPROVED_BY, GPT_APPROVED_DATE,
						    NO_OF_BAGS_CARTONS_QTY, NO_OF_BAGS_CARTONS_UNIT,
						    PACKING_STATUS, INLINE_DONE, PACKING_LIST,
						    PPS_RELEASE_DATE, PPS_EXPIRY_DATE,
						    FIBER_CONTENT_ON_PO, FIBER_CONTENT_ON_TEST_REPORT, FIBER_CONTENT_ON_WC,
						    GSM_ON_PO, GSM_ON_TEST_REPORT, GSM_ON_BULK,
						    MEASUREMENT_SAMPLE_SIZE, MEASUREMENT_ATTACHMENT,
						    NO_OF_CHECK, NO_OF_CHECK_UNIT, NO_OF_PREPACK_CHECK,
						    WORKMANSHIP, DEFECTS_STATUS, INSPECTION_RESULT,
						    REMARK_COMMENT, NOTE, NAME_OF_FACTORY_REPRESENTATIVE,
						    TEST_REPORT_ATTACHMENT, GEN_ATTACHMENT_FIRST, GEN_ATTACHMENT_SECOND,
						    IS_COMPLETE, CREATED_BY, RESEAN_FOR_FAIL, COLOR_CODE, MFG_DATE, GPT_COMM_APPROVAL_DATE, 
							DATE_OF_COMPLETION, SEASON, AREA_OF_FAIL_GPTR, GPT_COMM_APPROVED_BY, IS_EDIT_BY_ADMIN,
							GPT_ELASTIC_NO,GPT_ELASTIC_STATUS,GPT_ELASTIC_APPROVED_BY,GPT_ELASTIC_APPROVED_DATE,CONFORMITY_SIZE,
							CONFORMITY_SIZE_STATUS,CONFORMITY_PREPACK,CONFORMITY_PREPACK_STATUS,EMBROIDERY_PF,ACCESSORIES_PF,
							BARCODE_PF,NO_PLASTIC_PF,BOGO_PF,THREAD_YN,BUTTON_YN,ZIPPER_YN,INTERLINING_YN,ELASTIC_YN,MEASUREMENT_SAMPLE_QTY,
							ZIPPER_RPT_NO,ZIPPER_STATUS,ZIPPER_APPROVED_BY,	ZIPPER_APPROVED_DATE,FIT,COMM_APPROVAL_DOC,DEBIT_AMOUNT,ADMIN_REMARK,
							ABORTIVE_REASON_IDS,OTHER_ABORTIVE_REASON, AMT_TRAVELLING, SELECTED_BAG_CARTONS_NO,
							PO_TOTAL_QTY_AT_COMPLETION, PO_FABRIC_AT_COMPLETION, PO_COLOR_AT_COMPLETION, PO_MRP_AT_COMPLETION
						)
						VALUES
						(
						    @INSPECTION_ID, @INSPECTOR_CODE,@SHIP_QTY,@AQL_ID, @GPT_NO, @GPT_STATUS,
						    @GPT_APPROVED_BY, @GPT_APPROVED_DATE,
						    @NO_OF_BAGS_CARTONS_QTY, @NO_OF_BAGS_CARTONS_UNIT,
						    @PACKING_STATUS, @INLINE_DONE, @PACKING_LIST,
						    @PPS_RELEASE_DATE, @PPS_EXPIRY_DATE,
						    @FIBER_CONTENT_ON_PO, @FIBER_CONTENT_ON_TEST_REPORT, @FIBER_CONTENT_ON_WC,
						    @GSM_ON_PO, @GSM_ON_TEST_REPORT, @GSM_ON_BULK,
						    @MEASUREMENT_SAMPLE_SIZE, @MEASUREMENT_ATTACHMENT,
						    @NO_OF_CHECK, @NO_OF_CHECK_UNIT, @NO_OF_PREPACK_CHECK,
						    @WORKMANSHIP, @DEFECTS_STATUS, @INSPECTION_RESULT,
						    @REMARK_COMMENT, @NOTE, @NAME_OF_FACTORY_REPRESENTATIVE,
						    @TEST_REPORT_ATTACHMENT, @GEN_ATTACHMENT_FIRST, @GEN_ATTACHMENT_SECOND,
						    @IS_COMPLETE, @USER_ID,@RESEAN_FOR_FAIL, @COLOR_CODE, @MFG_DATE, @GPT_COMM_APPROVAL_DATE, 
							@DATE_OF_COMPLETION, @SEASON, @AREA_OF_FAIL_GPTR, @GPT_COMM_APPROVED_BY, @IS_EDIT_BY_ADMIN,
							@GPT_ELASTIC_NO,@GPT_ELASTIC_STATUS,@GPT_ELASTIC_APPROVED_BY,@GPT_ELASTIC_APPROVED_DATE,@CONFORMITY_SIZE,
							@CONFORMITY_SIZE_STATUS,@CONFORMITY_PREPACK,@CONFORMITY_PREPACK_STATUS,@EMBROIDERY_PF,@ACCESSORIES_PF,
							@BARCODE_PF,@NO_PLASTIC_PF,@BOGO_PF,@THREAD_YN,@BUTTON_YN,@ZIPPER_YN,@INTERLINING_YN,@ELASTIC_YN,@MEASUREMENT_SAMPLE_QTY,
							@ZIPPER_RPT_NO,@ZIPPER_STATUS,@ZIPPER_APPROVED_BY,@ZIPPER_APPROVED_DATE,@FIT,@COMM_APPROVAL_DOC,@DEBIT_AMOUNT,@ADMIN_REMARK,
							@ABORTIVE_REASON_IDS,@OTHER_ABORTIVE_REASON, @AMT_TRAVELLING,@SELECTED_BAG_CARTONS_NO,
							IIF(@TAKE_SNAPSHOT = 1, @SNAP_QTY, NULL), IIF(@TAKE_SNAPSHOT = 1, @SNAP_FABRIC, NULL), IIF(@TAKE_SNAPSHOT = 1, @SNAP_COLOR, NULL), IIF(@TAKE_SNAPSHOT = 1, @SNAP_MRP, NULL)
						);
						
						SET @OUT_INSPECT_ID = SCOPE_IDENTITY();
					END
				-- =======================
				-- UPDATE
				-- =======================
				ELSE
					BEGIN

					   -- Is this an admin/supervisor editing someone else's (a child's) record?
					   IF((@ROLEID=1008 OR @ROLEID=1007) AND (@QA_EMP_CODE_IN_REQUEST != @EMP_CODE_IN_REPORT))
					        BEGIN
					            SET @IS_EDIT_BY_ADMIN = 1;
								SET @LAST_UPDATE_DATE_BY_ADMIN = GETDATE();
					        END
					   ELSE
					        BEGIN
					            SET @IS_EDIT_BY_ADMIN = 0
					        END
					   
					   IF (@IsAllreadyComplete = 0)
					      OR (@IsAllreadyComplete = 1 AND @ROLEID = 1008)
					      OR (@IsAllreadyComplete = 1 AND @ROLEID = 1007 AND @QA_EMP_CODE_IN_REQUEST != @EMP_CODE_IN_REPORT)
					        BEGIN
					   
					            IF (@IsAllreadyComplete = 0)
					                BEGIN
					                    INSERT INTO TESTAUTOSAVE (USERID, INSPECITON_ID, IS_COMPLETE, DATEOFCOMPLETION, TESTVALUES)
					                    VALUES (@USER_ID,@INSPECT_ID, @IS_COMPLETE, @DATE_OF_COMPLETION, 'F-:'+@TEST_REPORT_ATTACHMENT+' :-|-: S-:'+@GEN_ATTACHMENT_FIRST);
					                END
					   
					            UPDATE dbo.INSPECTIONS
					            SET
					                AQL_ID=@AQL_ID,
					                OFFERED_QTY= @SHIP_QTY,
					                GPT_NO=@GPT_NO,
					                GPT_STATUS = @GPT_STATUS,
					                GPT_APPROVED_BY = @GPT_APPROVED_BY,
					                GPT_APPROVED_DATE = @GPT_APPROVED_DATE,
					                NO_OF_BAGS_CARTONS_QTY=@NO_OF_BAGS_CARTONS_QTY,
					                NO_OF_BAGS_CARTONS_UNIT=@NO_OF_BAGS_CARTONS_UNIT,
					                PACKING_STATUS = @PACKING_STATUS,
					                INLINE_DONE = @INLINE_DONE,
					                PACKING_LIST = @PACKING_LIST,
					                PPS_RELEASE_DATE = @PPS_RELEASE_DATE,
					                PPS_EXPIRY_DATE = @PPS_EXPIRY_DATE,
					                FIBER_CONTENT_ON_PO=@FIBER_CONTENT_ON_PO,
					                FIBER_CONTENT_ON_TEST_REPORT=@FIBER_CONTENT_ON_TEST_REPORT,
					                FIBER_CONTENT_ON_WC=@FIBER_CONTENT_ON_WC,
					                GSM_ON_PO=@GSM_ON_PO,
					                GSM_ON_TEST_REPORT=@GSM_ON_TEST_REPORT,
					                GSM_ON_BULK=@GSM_ON_BULK,
					                MEASUREMENT_SAMPLE_SIZE=@MEASUREMENT_SAMPLE_SIZE,
					                MEASUREMENT_ATTACHMENT=@MEASUREMENT_ATTACHMENT,
					                NO_OF_CHECK=@NO_OF_CHECK,
					                NO_OF_CHECK_UNIT=@NO_OF_CHECK_UNIT,
					                NO_OF_PREPACK_CHECK=@NO_OF_PREPACK_CHECK,
					                WORKMANSHIP=@WORKMANSHIP,
					                DEFECTS_STATUS = @DEFECTS_STATUS,
					                INSPECTION_RESULT = @INSPECTION_RESULT,
					                REMARK_COMMENT = @REMARK_COMMENT,
					                NOTE = @NOTE,
					                NAME_OF_FACTORY_REPRESENTATIVE=@NAME_OF_FACTORY_REPRESENTATIVE,
					                TEST_REPORT_ATTACHMENT=@TEST_REPORT_ATTACHMENT,
					                GEN_ATTACHMENT_FIRST=@GEN_ATTACHMENT_FIRST,
					                GEN_ATTACHMENT_SECOND=@GEN_ATTACHMENT_SECOND,
					                IS_COMPLETE = @IS_COMPLETE,
					                MODIFIED_BY = @USER_ID,
					                MODIFIED_DATE = GETDATE(),
					                RESEAN_FOR_FAIL=@RESEAN_FOR_FAIL,
					                COLOR_CODE=@COLOR_CODE,
					                MFG_DATE=@MFG_DATE,
					                GPT_COMM_APPROVAL_DATE=@GPT_COMM_APPROVAL_DATE,
					                DATE_OF_COMPLETION=@DATE_OF_COMPLETION,
					                SEASON=@SEASON,
					                AREA_OF_FAIL_GPTR=@AREA_OF_FAIL_GPTR,
					                GPT_COMM_APPROVED_BY=@GPT_COMM_APPROVED_BY,
					                IS_EDIT_BY_ADMIN=@IS_EDIT_BY_ADMIN,
									LAST_UPDATE_DATE_BY_ADMIN=@LAST_UPDATE_DATE_BY_ADMIN,
					   
					                GPT_ELASTIC_NO=@GPT_ELASTIC_NO,
					                GPT_ELASTIC_STATUS=@GPT_ELASTIC_STATUS,
					                GPT_ELASTIC_APPROVED_BY=@GPT_ELASTIC_APPROVED_BY,
					                GPT_ELASTIC_APPROVED_DATE=@GPT_ELASTIC_APPROVED_DATE,
					                CONFORMITY_SIZE=@CONFORMITY_SIZE,
					                CONFORMITY_SIZE_STATUS=@CONFORMITY_SIZE_STATUS,
					                CONFORMITY_PREPACK=@CONFORMITY_PREPACK,
					                CONFORMITY_PREPACK_STATUS=@CONFORMITY_PREPACK_STATUS,
					                EMBROIDERY_PF=@EMBROIDERY_PF,
					                ACCESSORIES_PF=@ACCESSORIES_PF,
					                BARCODE_PF=@BARCODE_PF,
					                NO_PLASTIC_PF=@NO_PLASTIC_PF,
					                BOGO_PF=@BOGO_PF,
					                THREAD_YN=@THREAD_YN,
					                BUTTON_YN=@BUTTON_YN,
					                ZIPPER_YN=@ZIPPER_YN,
					                INTERLINING_YN=@INTERLINING_YN,
					                ELASTIC_YN=@ELASTIC_YN,
					                MEASUREMENT_SAMPLE_QTY=@MEASUREMENT_SAMPLE_QTY,
					   
					                ZIPPER_RPT_NO=@ZIPPER_RPT_NO,
					                ZIPPER_STATUS=@ZIPPER_STATUS,
					                ZIPPER_APPROVED_BY=@ZIPPER_APPROVED_BY,
					                ZIPPER_APPROVED_DATE=@ZIPPER_APPROVED_DATE,
					                FIT=@FIT,
					                COMM_APPROVAL_DOC=@COMM_APPROVAL_DOC,
					                DEBIT_AMOUNT=@DEBIT_AMOUNT,
					                ADMIN_REMARK=@ADMIN_REMARK,
					                ABORTIVE_REASON_IDS=@ABORTIVE_REASON_IDS,
					                OTHER_ABORTIVE_REASON=@OTHER_ABORTIVE_REASON,
					                AMT_TRAVELLING=@AMT_TRAVELLING,
					                SELECTED_BAG_CARTONS_NO=@SELECTED_BAG_CARTONS_NO,
					                PO_TOTAL_QTY_AT_COMPLETION = IIF(@TAKE_SNAPSHOT = 1, @SNAP_QTY,    PO_TOTAL_QTY_AT_COMPLETION),
					                PO_FABRIC_AT_COMPLETION    = IIF(@TAKE_SNAPSHOT = 1, @SNAP_FABRIC, PO_FABRIC_AT_COMPLETION),
					                PO_COLOR_AT_COMPLETION     = IIF(@TAKE_SNAPSHOT = 1, @SNAP_COLOR,  PO_COLOR_AT_COMPLETION),
					                PO_MRP_AT_COMPLETION       = IIF(@TAKE_SNAPSHOT = 1, @SNAP_MRP,    PO_MRP_AT_COMPLETION)
					            WHERE INSPECT_ID = @INSPECT_ID;
					   
					            SET @OUT_INSPECT_ID = @INSPECT_ID;
					        END
					   ELSE
					        BEGIN
					            -- No permission: record is complete and caller is neither
					            -- 1008 admin nor a 1007 supervisor editing a child's record.
					            IF @@TRANCOUNT > 0
					                ROLLBACK TRAN;
					   
					            SET @ERROR_CODE = 50004;
					            SET @ERROR_MESSAGE = 'Inspection is already complete. Only Admin (1008) or a Supervisor (1007) editing a child inspector''s record can modify it.';
					            RETURN;
					        END
					END

				 /* =============================
				    MERGE DEFECTS
				 ==============================*/
				 MERGE dbo.INSPECTION_DEFECTS AS T
				 USING @DEFECTS AS S
				 ON T.DEFECTS_ID = S.DEFECTS_ID  AND T.INSPECTION_ID = @INSPECTION_ID
				 
				 WHEN MATCHED THEN
				     UPDATE SET
				         T.DEFECTS = S.DEFECTS,
				         T.DIMENSION = S.DIMENSION,
				         T.MAJOR = S.MAJOR,
				         T.MINOR = S.MINOR,
				         T.MODIFIED_BY = @USER_ID,
				         T.MODIFIED_DATE = GETDATE()
				 
				 WHEN NOT MATCHED BY TARGET THEN
				     INSERT
				     (
				         INSPECTION_ID, DEFECTS, DIMENSION,
				         MAJOR, MINOR, CREATED_BY, CREATED_DATE
				     )
				     VALUES
				     (
				         @INSPECTION_ID, S.DEFECTS, S.DIMENSION,
				         S.MAJOR, S.MINOR, @USER_ID, GETDATE()
				     )
				 
				 WHEN NOT MATCHED BY SOURCE  AND T.INSPECTION_ID = @INSPECTION_ID
				 THEN DELETE;
				 
				 IF( @IS_COMPLETE =1)
				 BEGIN
				 	IF( @INSPECTION_RESULT=4)
				 		BEGIN
				 			UPDATE INSPECTION_REQUESTS	SET STATUS_CODE=10, LAST_UPDATED_ON = GETDATE(), UPDATED_BY = @USER_ID WHERE INSP_ID = @INSPECTION_ID;
				 		END
				 	ELSE
				 		BEGIN
				 			UPDATE INSPECTION_REQUESTS	SET STATUS_CODE=7, LAST_UPDATED_ON = GETDATE(), UPDATED_BY = @USER_ID WHERE INSP_ID = @INSPECTION_ID;
				 		END
				 END
			END
        COMMIT TRAN;

        SET @ERROR_CODE = 0;
        SET @ERROR_MESSAGE = 'Inspection saved successfully';
    END TRY
    BEGIN CATCH
	IF @@TRANCOUNT>0
        ROLLBACK TRAN;

        SET @ERROR_CODE = 1;
        SET @ERROR_MESSAGE = ERROR_MESSAGE();

    END CATCH
END



SET QUOTED_IDENTIFIER ON
GO

SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE OR ALTER PROCEDURE [dbo].[PRO_INSPECTION_DETAIL]
	@ID INT,
	@STRFIELD VARCHAR(50),
	@CMD VARCHAR(100)
AS
BEGIN
	SET NOCOUNT ON;
	DECLARE @SDATE DATETIME=GETDATE();
	IF(@CMD='Get_InspRequest_By_Id')
		BEGIN
			SELECT A.INSP_ID, A.POID,B.PO_NUMBER,
			ISNULL(B.LINK_PO_NUMBER,'') as LINK_PO_NUMBER,B.VENDOR_CODE,B.VENDOR_NAME,B.FABRIC,B.COLOR,B.TOTAL_QTY , A.OFFERED_QTY,A.DATE_OF_INSPECTION, 
			E.ADDRESS_LINE1 + E.CITY + E.REGION_DESCRIPTION+ E.PINCODE as LOCATION,A.ADDRESS_ID,
			A.CREATED_ON, C.VENDOR_NAME as CREATED_BY, D.VENDOR_NAME as UPDATED_BY, A.LAST_UPDATED_ON,A.REGION_ID,A.QA_ASSIGNED_DOI,
			CONVERT(int, A.INSPECTOR_EMP_CODE) as INSPECTOR_EMP_CODE, A.IS_DATE_ASSIGNED_BY_QA, A.IS_INSPECTOR_ASSIGNED,
			ISNULL(S.INSP_STATUS,'Created') as INSP_REQ_STATUS,A.INSPECTION_TYPE_ID,B.APPARELS,B.CATEGORY,B.BUYER_NAME,B.BUYER_ID,
			A.INSPECTION_NO, U.EMP_NAME as INSPECTOR,B.MC,B.MC_TEXT, B.MRP, B.BRAND_ID, B.BRAND_ID2,B.FIT,A.STATUS_CODE, A.IS_QC_AT_DC
			FROM [dbo].[INSPECTION_REQUESTS] A 
			left join DBO.POHEADER B on A.POID=B.POID
			left join [dbo].[USER_MASTER] U on A.INSPECTOR_EMP_CODE=U.EMP_CODE
			left join [dbo].[VENDORS_MASTER] C on A.CREATED_BY=C.VENDOR_ID
			left join [dbo].[VENDORS_MASTER] D on A.UPDATED_BY=D.VENDOR_ID
			left join DBO.VENDOR_ADDRESS E on A.ADDRESS_ID=E.ADDRESS_ID
			left join DBO.INSPECTION_REQUEST_STATUS S on A.STATUS_CODE=S.INSP_STATUS_ID
			WHERE  A.INSP_ID =@ID

			SELECT PO_DETAILS_ID, POID,LINE_ITEM,ARTICLE,ARTICLE_DESC,PREPACK_QTY,PREPACK_QTY_EACHES,PURCHASE_TOT_QTY,EACHES,MRP,ATTYP,
			CREATED_ON,
			LAST_UPDATED_ON,
			CREATED_BY,	isnull(UPDATED_BY,0) UPDATED_BY 
			FROM   [DBO].[PODETAIL] A WHERE POID=(SELECT POID FROM  [DBO].[INSPECTION_REQUESTS] WHERE INSP_ID =@ID) 
			
			SELECT ADDRESS_ID, VENDOR_ID,ADDRESS_LINE1,CITY,PINCODE,EMAIL_IDS,MOBILE_NUMBERS,CONTACT_PERSON,REGION_CODE,REGION_DESCRIPTION,IS_DEFAULT,
			CREATED_DATE,
			MODIFIED_DATE,
			CREATED_BY,	isnull(MODIFIED_BY,0) MODIFIED_BY 
			FROM   [DBO].[VENDOR_ADDRESS] A WHERE VENDOR_ID=(SELECT VENDOR_ID FROM  [DBO].[VENDORS_MASTER] WHERE VENDOR_CODE=(SELECT VENDOR_CODE FROM  [DBO].[POHEADER] WHERE POID=(SELECT POID FROM  [DBO].[INSPECTION_REQUESTS] WHERE INSP_ID =@ID) ) ) ORDER BY A.MODIFIED_DATE DESC
	
	   
		
		END
	IF(@CMD='INSPECT_DETAILS_BY_INSPECTION_ID')
		BEGIN
			SELECT I.INSPECT_ID,I.INSPECTION_ID,I.INSPECTOR_CODE, I.OFFERED_QTY,I.GPT_NO,I.GPT_STATUS,I.GPT_APPROVED_BY,I.GPT_APPROVED_DATE,I.NO_OF_BAGS_CARTONS_QTY,I.NO_OF_BAGS_CARTONS_UNIT,
			I.PACKING_STATUS,I.INLINE_DONE,I.PACKING_LIST,I.PPS_RELEASE_DATE,I.PPS_EXPIRY_DATE,I.FIBER_CONTENT_ON_PO, I.FIBER_CONTENT_ON_TEST_REPORT, I.FIBER_CONTENT_ON_WC, I.GSM_ON_PO,I.GSM_ON_TEST_REPORT,
			I.GSM_ON_BULK,I.MEASUREMENT_SAMPLE_SIZE,I.MEASUREMENT_ATTACHMENT, I.NO_OF_CHECK,I.NO_OF_CHECK_UNIT,NO_OF_PREPACK_CHECK,I.WORKMANSHIP,I.DEFECTS_STATUS,
			I.INSPECTION_RESULT,I.REMARK_COMMENT, I.NOTE, I.NAME_OF_FACTORY_REPRESENTATIVE, I.TEST_REPORT_ATTACHMENT,I.GEN_ATTACHMENT_FIRST,I.GEN_ATTACHMENT_SECOND, I.IS_COMPLETE,I.CREATED_BY,I.CREATED_DATE,
			I.MODIFIED_BY, I.MODIFIED_DATE, A.SAMPLE_SIZE as AQL_SAMPLE_SIZE, A.AQL_2_5_MAJOR,A.AQL_2_5_MINOR,
			I.RESEAN_FOR_FAIL,	I.COLOR_CODE,	I.MFG_DATE,	I.GPT_COMM_APPROVAL_DATE,	I.SEASON,	I.AREA_OF_FAIL_GPTR,	I.GPT_COMM_APPROVED_BY,
			GPT_ELASTIC_NO,GPT_ELASTIC_STATUS,GPT_ELASTIC_APPROVED_BY,GPT_ELASTIC_APPROVED_DATE,CONFORMITY_SIZE,CONFORMITY_SIZE_STATUS,CONFORMITY_PREPACK,
			CONFORMITY_PREPACK_STATUS,EMBROIDERY_PF	,ACCESSORIES_PF,BARCODE_PF,NO_PLASTIC_PF,BOGO_PF,THREAD_YN,BUTTON_YN,ZIPPER_YN,INTERLINING_YN,ELASTIC_YN,MEASUREMENT_SAMPLE_QTY,
			I.DATE_OF_COMPLETION,
			ZIPPER_RPT_NO,ZIPPER_STATUS,ZIPPER_APPROVED_BY,ZIPPER_APPROVED_DATE,I.FIT,COMM_APPROVAL_DOC,DEBIT_AMOUNT,ADMIN_REMARK,
			I.ABORTIVE_REASON_IDS,I.OTHER_ABORTIVE_REASON ,I.AMT_TRAVELLING ,I.SELECTED_BAG_CARTONS_NO,I.LAST_UPDATE_DATE_BY_ADMIN 
			FROM INSPECTIONS I 
			LEFT JOIN MASTER_AQL A ON  I.AQL_ID=A.AQL_ID
			WHERE I.INSPECTION_ID=@ID

			SELECT  DEFECTS_ID, INSPECTION_ID, DEFECTS, DIMENSION, MAJOR, MINOR
			FROM INSPECTION_DEFECTS WHERE INSPECTION_ID=@ID

			SELECT BATCH_SIZE_MIN, BATCH_SIZE_MAX, SAMPLE_SIZE,AQL_2_5_MAJOR,AQL_2_5_MINOR  FROM MASTER_AQL ORDER BY AQL_ID
		END
	IF(@CMD='INSPECTION_REPORT_PDF')
		BEGIN
			
			SET NOCOUNT ON;

			;WITH PODETAIL_CTE AS
			(
				SELECT PD.POID, STRING_AGG(CONCAT(PD.Article, '~',CAST( ISNULL(PD.MRP,0) as INT), '/-'), ' | ')  WITHIN GROUP (ORDER BY PD.LINE_ITEM) AS ARTICLES
				FROM PODETAIL PD
				INNER JOIN INSPECTION_REQUESTS ir ON ir.POID = PD.POID
				INNER JOIN INSPECTIONS i ON i.INSPECTION_ID = ir.INSP_ID
				WHERE  PD.LINE_ITEM % 100 = 0 AND i.INSPECT_ID = @ID
				GROUP BY PD.POID
			)

			SELECT

			    I.DATE_OF_COMPLETION,
				ISNULL(A.INSPECTION_NO,'NA') AS INSPECTION_REPORT_NO,
				IT.INSPECTION_TYPE AS TYPE_OF_INSPECTION,
				P.PASSFAIL AS INSPECTION_RESULT,

				CASE WHEN ISNULL(B.BUYER_NAME,'') <> '' AND ISNULL(B.BUYER_ID,'') <> '' THEN B.BUYER_NAME + '[' + B.BUYER_ID + ']' ELSE '-' END AS BUYER_NAME,
				CONCAT (B.PO_NUMBER, CASE WHEN ISNULL(B.LINK_PO_NUMBER,'') <> '' THEN ' ~ ' + B.LINK_PO_NUMBER ELSE '' END ) AS PO_NUMBER,

				B.VENDOR_CODE,
				B.VENDOR_NAME,
				E.ADDRESS_LINE1 +', '+ E.CITY +', '+ E.REGION_DESCRIPTION +', '+ E.PINCODE+ ', '+RM.REGION AS LOCATION,
				E.ADDRESS_LINE1 +', '+ E.CITY +', '+ E.REGION_DESCRIPTION +', '+ E.PINCODE AS ADDRESS,
				CAST(COALESCE(I.PO_TOTAL_QTY_AT_COMPLETION, B.TOTAL_QTY) AS FLOAT) AS PO_QTY,   -- E12: values frozen at completion
				
				B.MC_TEXT AS DESCRIPTION,
				B.FIT AS PO_FIT,
				COALESCE(I.PO_MRP_AT_COMPLETION, B.MRP) AS MRP,

				CAST(A.OFFERED_QTY AS FLOAT) AS OFFERED_QTY,
				CAST(I.OFFERED_QTY AS FLOAT) AS SHIP_QTY,

				B.CATEGORY,
				CASE WHEN ISNULL(B.BRAND_ID2,'') <> '' AND ISNULL(B.BRAND_ID,'') <> '' THEN B.BRAND_ID2 + ' [ ' + B.BRAND_ID + ' ]' ELSE '-' END AS BRAND_LABEL,
				COALESCE(I.PO_COLOR_AT_COMPLETION, B.COLOR) AS COLOR,
				PD.ARTICLES ,
			   
			    I.GPT_NO AS GPT_NO,
				CASE WHEN I.GPT_STATUS > 0 THEN GSPF.PASSFAIL ELSE '' END AS GPT_STATUS,
				I.GPT_APPROVED_BY AS APPROVED_BY_IF_GPT_FAILS,
				I.GPT_APPROVED_DATE AS APPROVED_DATE,
				I.AREA_OF_FAIL_GPTR AS AREA_OF_FAILURE_GPTR	,

				I.GPT_ELASTIC_NO,
				CASE WHEN I.GPT_ELASTIC_STATUS > 0 THEN ESPF.PASSFAIL ELSE '' END AS ELASTIC_STATUS,
				I.GPT_ELASTIC_APPROVED_BY,
				I.GPT_ELASTIC_APPROVED_DATE,

				I.ZIPPER_RPT_NO AS ZIPPER_REPORT_NO,
				CASE WHEN I.ZIPPER_STATUS > 0 THEN ZSPF.PASSFAIL ELSE '' END AS ZIPPER_REPORT_STATUS,
				I.ZIPPER_APPROVED_BY AS IF_FAIL_ZIPPER_APPROVED_BY,
				I.ZIPPER_APPROVED_DATE,
				
				CASE WHEN I.NO_OF_BAGS_CARTONS_QTY > 0 THEN CAST(I.NO_OF_BAGS_CARTONS_QTY AS VARCHAR) + ' ' + ISNULL(UNOB.UNIT,'') ELSE ''
				END AS NO_OF_BAGS_CARTON,
				CASE WHEN I.PACKING_STATUS > 0 THEN PSYN.YESNO ELSE '' END AS PACKING_STATUS,

				CASE WHEN I.INLINE_DONE > 0 THEN IDYN.YESNO ELSE '' END AS INLINE_DONE,
				CASE WHEN I.PACKING_LIST > 0 THEN PLYN.YESNO ELSE '' END AS PACKING_LIST,
				
				CONVERT(VARCHAR(11), I.PPS_RELEASE_DATE, 106) AS PPS_RELEASE_DATE,
				CONVERT(VARCHAR(11), I.PPS_EXPIRY_DATE, 106) AS PPS_EXPIRY_DATE,
				CONVERT(VARCHAR(11), I.MFG_DATE, 106) AS MFG_DATE,

				I.SEASON,
				CC.COLOR_CODE,

				I.FIBER_CONTENT_ON_PO,
				I.FIBER_CONTENT_ON_TEST_REPORT,
				I.FIBER_CONTENT_ON_WC AS FIBER_CONTENT_ON_LABEL,
				
				I.GSM_ON_PO,
				I.GSM_ON_TEST_REPORT,
				I.GSM_ON_BULK,

				I.FIT AS INSPECTION_FIT,
				ISNULL(I.SELECTED_BAG_CARTONS_NO,'') AS SELECTED_BAGS_CARTON_NO,

				ISNULL(MFR.FAIL_REASON,'') AS FAIL_REASON,
				MAQL.SAMPLE_SIZE,
				

				I.CONFORMITY_SIZE,
				CASE WHEN I.CONFORMITY_SIZE_STATUS > 0 THEN  SSPF.PASSFAIL ELSE '' END AS CONFORMITY_SIZE_STATUS,
				CASE WHEN I.EMBROIDERY_PF > 0 THEN EMPF.PASSFAIL ELSE '' END AS EMBRIODERY_PRINT_SHOULD_BE_AS_PER_PPS,
				CASE WHEN I.ACCESSORIES_PF > 0 THEN ACPF.PASSFAIL ELSE '' END AS ACCESSORIES_SHOULD_BE_AS_PER_PPS,
				CASE WHEN I.BARCODE_PF > 0 THEN BAPF.PASSFAIL ELSE '' END AS BARCODE_COMPLIANCES,
				CASE WHEN I.NO_PLASTIC_PF > 0 THEN NPPF.PASSFAIL ELSE '' END AS NO_PLASTIC_TAG_KIMBLE_TAG,
				CASE WHEN I.BOGO_PF > 0 THEN BOPF.PASSFAIL ELSE '' END AS BOGO_DISCOUNT_TAG,

				I.CONFORMITY_PREPACK ,
				CASE WHEN I.CONFORMITY_PREPACK_STATUS > 0 THEN PPRYN.YESNO ELSE '' END AS CONFORMITY_PREPACK_STATUS,
				CASE WHEN I.THREAD_YN > 0 THEN THYN.YESNO ELSE '' END AS THREAD_FROM_NOMINATED_Y_N,
				CASE WHEN I.BUTTON_YN > 0 THEN BUYN.YESNO ELSE '' END AS BUTTON_FROM_NOMINATED_Y_N,
				CASE WHEN I.ZIPPER_YN > 0 THEN ZIYN.YESNO ELSE '' END AS ZIPPER_FROM_NOMINATED_Y_N,
				CASE WHEN I.INTERLINING_YN > 0 THEN INYN.YESNO ELSE '' END AS INTERLINING_FROM_NOMINATED_VENDOR_Y_N,
				CASE WHEN I.ELASTIC_YN > 0 THEN ELYN.YESNO ELSE '' END AS ELASTIC_FROM_NOMINATED_Y_N,

				I.MEASUREMENT_SAMPLE_QTY AS MESUREMENT_SAMPLE_SIZE,
				CASE WHEN I.MEASUREMENT_SAMPLE_SIZE > 0 THEN MPF.PASSFAIL ELSE '' END AS MEASUREMENT_PASS_FAIL,

				MAQL.AQL_2_5_MAJOR AS ALLOWED_MAJOR,
				MAQL.AQL_2_5_MINOR AS ALLOWED_MINOR,

				CASE WHEN I.NO_OF_CHECK > 0 THEN CAST(I.NO_OF_CHECK AS VARCHAR) + ' ' + ISNULL(UCOB.UNIT,'') ELSE '0 BAG' END AS NO_OF_CHECK_CARTON_BAG,
				I.NO_OF_PREPACK_CHECK AS NO_OF_CHECK_PRE_PACK,
				
				CONVERT(VARCHAR(11), I.GPT_COMM_APPROVAL_DATE,106) AS FI_COMM_APPR_DATE,
				ISNULL(I.GPT_COMM_APPROVED_BY,'') AS FI_COMM_APPROVED_BY,
				ISNULL(I.COMM_APPROVAL_DOC,'') AS COMM_APPROVAL_DOCUMENTS,
				I.DEBIT_AMOUNT,
				I.AMT_TRAVELLING,
				ISNULL(I.ADMIN_REMARK,'') AS ADMIN_REMARK,

				ISNULL(I.REMARK_COMMENT,'') AS REMARKS_DETAILS,
				ISNULL(I.NOTE,'') AS NOTE ,

				CASE  WHEN ISNULL(A.INSPECTOR_EMP_CODE,'') <> '' AND ISNULL(INSP.EMP_NAME,'') <> '' THEN INSP.EMP_NAME + ' [ ' + A.INSPECTOR_EMP_CODE + ' ]' ELSE '-' END AS NAME_OF_THE_VMM_QA,
				INSP.STAMP_NO,
				INSP.SIGN,
				I.NAME_OF_FACTORY_REPRESENTATIVE,

				CASE  WHEN ISNULL(ADMN.EMP_NAME,'') <> '' THEN ADMN.EMP_NAME + ' [ ' + ADMN.EMP_CODE + ' ]' ELSE '---' END AS NAME_OF_QA_ADMIN,
				ADMN.STAMP_NO AS QAADMIN_STAMP_NO,
				ADMN.SIGN AS QAADMIN_SIGN,
				

				I.TEST_REPORT_ATTACHMENT,
				I.GEN_ATTACHMENT_FIRST,
				
				I.IS_COMPLETE,
				I.IS_EDIT_BY_ADMIN,

				I.ABORTIVE_REASON_IDS,
				I.OTHER_ABORTIVE_REASON,

				B.APPARELS AS APPS_NON_APPS,
				E.CONTACT_PERSON AS CONTACT_NAME,
				E.MOBILE_NUMBERS AS CONTACT_NUMBER,
				
				I.INSPECT_ID,
				A.INSP_ID,
				A.POID,
				CONVERT(VARCHAR(11), A.DATE_OF_INSPECTION, 106) AS INSPECTION_DATE,
				A.INSPECTION_TYPE_ID,
				I.LAST_UPDATE_DATE_BY_ADMIN

			FROM INSPECTIONS I

			INNER JOIN INSPECTION_REQUESTS A
				ON I.INSPECTION_ID= A.INSP_ID
			
			INNER JOIN PODETAIL_CTE PD ON A.POID = PD.POID 
				
			LEFT JOIN MASTER_REGION RM 
				ON A.REGION_ID = RM.REGION_ID

			LEFT JOIN POHEADER B 
				ON A.POID = B.POID

			LEFT JOIN VENDOR_ADDRESS E
				ON A.ADDRESS_ID = E.ADDRESS_ID

			LEFT JOIN MASTER_PASSFAIL P
				ON I.INSPECTION_RESULT = P.PASSFAIL_ID

			LEFT JOIN USER_MASTER INSP
				ON A.INSPECTOR_EMP_CODE = INSP.EMP_CODE

			LEFT JOIN USER_MASTER ADMN
				ON I.MODIFIED_BY = ADMN.ID AND I.IS_EDIT_BY_ADMIN=1  AND (ADMN.ROLE_ID=1008 OR ADMN.ROLE_ID=1007)

			LEFT JOIN MASTER_INSPECTION_TYPE IT
				ON A.INSPECTION_TYPE_ID = IT.INSPECTION_TYPE_ID

			LEFT JOIN MASTER_FAIL_REASON MFR
				ON I.RESEAN_FOR_FAIL = MFR.FAIL_REASON_ID

			LEFT JOIN MASTER_COLOR_CODE CC
				ON I.COLOR_CODE = CC.COLOR_CODE_ID

			LEFT JOIN MASTER_UNIT UNOB
				ON I.NO_OF_BAGS_CARTONS_UNIT = UNOB.UNIT_ID

			LEFT JOIN MASTER_UNIT UCOB
				ON I.NO_OF_CHECK_UNIT = UCOB.UNIT_ID

			LEFT JOIN MASTER_PASSFAIL MPF
				ON I.MEASUREMENT_SAMPLE_SIZE = MPF.PASSFAIL_ID

			LEFT JOIN MASTER_YESNO PSYN
				ON I.PACKING_STATUS = PSYN.YESNO_ID

			LEFT JOIN MASTER_YESNO PLYN
				ON I.PACKING_LIST = PLYN.YESNO_ID

			LEFT JOIN MASTER_YESNO IDYN
				ON I.INLINE_DONE = IDYN.YESNO_ID

			LEFT JOIN MASTER_PASSFAIL GSPF
				ON I.GPT_STATUS = GSPF.PASSFAIL_ID

			LEFT JOIN MASTER_PASSFAIL ESPF
				ON I.GPT_ELASTIC_STATUS = ESPF.PASSFAIL_ID

			LEFT JOIN MASTER_PASSFAIL ZSPF
				ON I.ZIPPER_STATUS = ZSPF.PASSFAIL_ID

			LEFT JOIN MASTER_PASSFAIL SSPF
				ON I.CONFORMITY_SIZE_STATUS = SSPF.PASSFAIL_ID

			LEFT JOIN MASTER_PASSFAIL EMPF
				ON I.EMBROIDERY_PF = EMPF.PASSFAIL_ID

			LEFT JOIN MASTER_PASSFAIL ACPF
				ON I.ACCESSORIES_PF = ACPF.PASSFAIL_ID

			LEFT JOIN MASTER_PASSFAIL BAPF
				ON I.BARCODE_PF = BAPF.PASSFAIL_ID

			LEFT JOIN MASTER_PASSFAIL NPPF
				ON I.NO_PLASTIC_PF = NPPF.PASSFAIL_ID

			LEFT JOIN MASTER_PASSFAIL BOPF
				ON I.BOGO_PF = BOPF.PASSFAIL_ID

			LEFT JOIN MASTER_YESNO PPRYN
				ON I.CONFORMITY_PREPACK_STATUS = PPRYN.YESNO_ID

			LEFT JOIN MASTER_YESNO THYN
				ON I.THREAD_YN = THYN.YESNO_ID

			LEFT JOIN MASTER_YESNO BUYN
				ON I.BUTTON_YN = BUYN.YESNO_ID

			LEFT JOIN MASTER_YESNO ZIYN
				ON I.ZIPPER_YN = ZIYN.YESNO_ID

			LEFT JOIN MASTER_YESNO INYN
				ON I.INTERLINING_YN = INYN.YESNO_ID

			LEFT JOIN MASTER_YESNO ELYN
				ON I.ELASTIC_YN = ELYN.YESNO_ID

			LEFT JOIN MASTER_AQL MAQL
				ON I.OFFERED_QTY BETWEEN MAQL.BATCH_SIZE_MIN AND MAQL.BATCH_SIZE_MAX

			WHERE I.INSPECT_ID = @ID


			--DEFECTS--
			SELECT D.DEFECTS_ID,  D.DEFECTS,MAJOR, MINOR 
			FROM INSPECTION_DEFECTS D
			INNER JOIN INSPECTION_REQUESTS IR on D.INSPECTION_ID=IR.INSP_ID
			INNER JOIN INSPECTIONS I ON IR.INSP_ID = I.INSPECTION_ID
			WHERE I.INSPECT_ID = @ID

			--ABORTIVE REASONS--
			SELECT AR_ID,REASON FROM MASTER_ABORTIVE_REASON MAR
			
		END

	--INSERT INTO [dbo].[TESTDESHBOARDLIST]([START_TIME],[INSP_IDS],[OUTPUT_TIME],[EXECUTION_TIME_IN_MS])
	--VALUES (@SDATE,'CMD : '+ @CMD +', STRFIELD : '+@STRFIELD+', | ID : '+ CAST(@ID as VARCHAR) ,GETDATE(),DATEDIFF(MILLISECOND, @SDATE, GETDATE()))
END
GO
