/*
    QPS — prerequisites for a TEST database only. Run BEFORE Database/qps.sql.

    Database/qps.sql refers to two things that live outside the QPS database in production:
      1. dbo.APPS_BSR — the BSR table (used by view VW_BSR_WITHIMAGEURL, function GetCurrentWeek and
         PRO_INSERT_TBL_FROM_TEMP). It is not part of the export, so the view cannot be created without it.
      2. Linked server [192.168.0.43] (OMS) — read by PRO_INSERT_UPDATE_MATERIAL_IMAGE_URL. SQL Server refuses
         to create the procedure when the linked server is not defined.

    This script creates a minimal stand-in for both so qps.sql runs end to end on an empty server.
    Do NOT run it on the production server: it would add a fake linked server and an empty APPS_BSR table.

    sqlcmd -S <server> -d <test db> -b -i Database/test/00-test-prerequisites.sql
*/
SET NOCOUNT ON;

-- 1. Stand-in for the BSR table (only the columns the QPS objects use by name).
IF OBJECT_ID(N'dbo.APPS_BSR', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.APPS_BSR
    (
        MATERIAL      VARCHAR(50)  NOT NULL,
        CALENDAR_WEEK VARCHAR(10)  NOT NULL,
        SEASON        VARCHAR(50)  NULL,
        DIV           VARCHAR(10)  NULL,
        MC            VARCHAR(50)  NULL,
        BRAND_TYPE    VARCHAR(50)  NULL,
        VENDOR        VARCHAR(50)  NULL,
        MRP           NUMERIC(18, 4) NULL,
        LOADED_ON     DATETIME     NULL
    );
END
GO

-- 2. Linked-server name used by PRO_INSERT_UPDATE_MATERIAL_IMAGE_URL. SQL Server connects through it when the
--    procedure is created and checks that OMS.dbo.TBL_ARTICLE_MST exists, so the linked server points back at
--    this server and a stand-in OMS database holds that table. Needs sysadmin (linked server, CREATE DATABASE).
IF NOT EXISTS (SELECT 1 FROM sys.servers WHERE name = N'192.168.0.43')
    EXEC master.dbo.sp_addlinkedserver @server = N'192.168.0.43', @srvproduct = N'', @provider = N'MSOLEDBSQL', @datasrc = N'localhost';
GO
IF DB_ID(N'OMS') IS NULL
    CREATE DATABASE OMS;
GO
IF OBJECT_ID(N'OMS.dbo.TBL_ARTICLE_MST', N'U') IS NULL
    EXEC (N'CREATE TABLE OMS.dbo.TBL_ARTICLE_MST (SKU_CODE VARCHAR(50) NOT NULL PRIMARY KEY, IMAGE_URL VARCHAR(500) NULL, LAST_UPDATED_DATE DATETIME NULL);');
GO
