/*
    2026-11-23 — security: stop the password-reset procedures writing token hashes to the debug table ProcParaTest.

    auth.PRO_PASSWORD_RESET_TOKEN_ISSUE, _FIND and _REDEEM each inserted the token hash (and the user code) into
    dbo.ProcParaTest on every call, a leftover from debugging. The procedures are recreated without it, and the rows
    they already logged are deleted. Nothing else uses ProcParaTest, so the table itself is left in place.

    Run once on each existing QPS database (new databases get the fixed procedures from Database/qps.sql);
    re-running it is harmless:
        sqlcmd -S <server> -d VMM_QPS -b -i Database/migrations/2026-11-23-remove-reset-token-debug-log.sql
*/

SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE OR ALTER PROCEDURE [auth].[PRO_PASSWORD_RESET_TOKEN_ISSUE]
    @ACCOUNT_TYPE  VARCHAR(20),
    @USER_CODE     VARCHAR(10),
    @TOKEN_HASH    VARCHAR(128),        -- match your column: e.g. NVARCHAR(128) if you store hex/Base64
    @EXPIRES_UTC   DATETIMEOFFSET,
    @NOW_UTC       DATETIMEOFFSET
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    BEGIN TRY
        BEGIN TRANSACTION;
        -- Only one live token per user: issuing a new one invalidates the previous link.
        -- UPDLOCK + HOLDLOCK keeps two simultaneous "forgot password" clicks from
        -- both inserting and leaving two valid links.
        DELETE t
          FROM auth.PasswordResetToken AS t WITH (UPDLOCK, HOLDLOCK)
         WHERE t.UserId      = @USER_CODE
           AND t.AccountType = @ACCOUNT_TYPE;

        INSERT INTO auth.PasswordResetToken (TokenHash, UserId, AccountType, ExpiresUtc, CreatedUtc)
        VALUES (@TOKEN_HASH, @USER_CODE, @ACCOUNT_TYPE, @EXPIRES_UTC, @NOW_UTC);

        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        THROW;
    END CATCH

    -- Housekeeping outside the transaction: expired tokens of ALL users.
    -- Kept separate so it never holds locks on other users' rows during the issue.
    DELETE FROM auth.PasswordResetToken
     WHERE ExpiresUtc <= @NOW_UTC;
END
GO

SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE OR ALTER PROCEDURE [auth].[PRO_PASSWORD_RESET_TOKEN_FIND]
    @TOKEN_HASH  VARCHAR(128),        -- same type as in PRO_PASSWORD_RESET_TOKEN_ISSUE
    @NOW_UTC     DATETIMEOFFSET
AS
BEGIN
    SET NOCOUNT ON;
    SELECT TOP (1)
           t.UserId      AS USER_CODE,
           t.AccountType AS ACCOUNT_TYPE
      FROM auth.PasswordResetToken AS t
     WHERE t.TokenHash  = @TOKEN_HASH
       AND t.ExpiresUtc > @NOW_UTC;
END
GO

SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE OR ALTER PROCEDURE [auth].[PRO_PASSWORD_RESET_TOKEN_REDEEM]
    @TOKEN_HASH  VARCHAR(128),        -- same type as ISSUE / FIND
    @NOW_UTC     DATETIMEOFFSET
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @Deleted TABLE
    (
        USER_CODE     VARCHAR(10),
        ACCOUNT_TYPE  VARCHAR(20),
        EXPIRES_UTC   DATETIMEOFFSET
    );
    -- Atomic: two requests racing with the same link can't both get a row back.
    -- Expired tokens are deleted too, but return no user.
    DELETE FROM auth.PasswordResetToken
    OUTPUT deleted.UserId, deleted.AccountType, deleted.ExpiresUtc
      INTO @Deleted (USER_CODE, ACCOUNT_TYPE, EXPIRES_UTC)
     WHERE TokenHash = @TOKEN_HASH;

    SELECT TOP (1) USER_CODE, ACCOUNT_TYPE
      FROM @Deleted
     WHERE EXPIRES_UTC > @NOW_UTC;
END
GO

-- Rows those three procedures already wrote (each contains 'Token :').
IF OBJECT_ID(N'dbo.ProcParaTest', N'U') IS NOT NULL
    DELETE FROM dbo.ProcParaTest WHERE ColumnValues LIKE '%Token :%';
GO
