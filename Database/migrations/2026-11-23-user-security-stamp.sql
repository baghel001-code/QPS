/*
    2026-11-23 — fix: a failed sign-in (or lockout) of a user who has no auth.UserSecurity row yet raised
    "Cannot insert the value NULL into column 'SecurityStamp'". Both procedures now give the new row a random
    SecurityStamp, the same way the other auth procedures do.

    Run once on each existing QPS database (new databases get the fixed procedures from Database/qps.sql):
        sqlcmd -S <server> -d VMM_QPS -b -i Database/migrations/2026-11-23-user-security-stamp.sql
*/

SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE OR ALTER PROCEDURE [auth].[PRO_LOGIN_LOCKOUT]
    @ACCOUNT_TYPE     VARCHAR(20),
    @USER_CODE        VARCHAR(10),
    @LOCKOUT_END_UTC  DATETIMEOFFSET,
    @NOW_UTC          DATETIMEOFFSET
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    BEGIN TRY
        BEGIN TRANSACTION;

        -- Lock the row (or the key range if it doesn't exist yet) so a concurrent
        -- failed-login call can't insert a duplicate between the UPDATE and INSERT.
        UPDATE s WITH (UPDLOCK, HOLDLOCK)
           SET s.FailedLoginCount = 0,          -- fresh attempts once the lockout expires
               s.LockoutEndUtc    = @LOCKOUT_END_UTC,
               s.UpdatedUtc       = @NOW_UTC
          FROM auth.UserSecurity AS s
         WHERE s.UserId      = @USER_CODE
           AND s.AccountType = @ACCOUNT_TYPE;

        -- Replaces EnsureSecurityRow: create the row already locked out.
        IF @@ROWCOUNT = 0
        BEGIN
            INSERT INTO auth.UserSecurity (UserId, AccountType, SecurityStamp, FailedLoginCount, LockoutEndUtc, UpdatedUtc)
            VALUES (@USER_CODE, @ACCOUNT_TYPE, CONVERT(NVARCHAR(64), NEWID()), 0, @LOCKOUT_END_UTC, @NOW_UTC);   -- SecurityStamp is NOT NULL
        END

        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        THROW;
    END CATCH
END
GO

SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE OR ALTER PROCEDURE [auth].[PRO_LOGIN_RECORD_FAILED_COUNT]
    @ACCOUNT_TYPE  VARCHAR(20),
    @USER_CODE     VARCHAR(10),
    @NOW_UTC       DATETIMEOFFSET
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    DECLARE @Result TABLE (FAILED_LOGIN_COUNT INT);

    BEGIN TRY
        BEGIN TRANSACTION;

        -- UPDLOCK + HOLDLOCK: two simultaneous wrong-password attempts can't both
        -- miss the row and insert twice, and increments are never lost.
        UPDATE s WITH (UPDLOCK, HOLDLOCK)
           SET s.FailedLoginCount = ISNULL(s.FailedLoginCount, 0) + 1,
               s.UpdatedUtc       = @NOW_UTC
        OUTPUT inserted.FailedLoginCount INTO @Result (FAILED_LOGIN_COUNT)
          FROM auth.UserSecurity AS s
         WHERE s.UserId      = @USER_CODE
           AND s.AccountType = @ACCOUNT_TYPE;

        IF @@ROWCOUNT = 0
        BEGIN
            INSERT INTO auth.UserSecurity (UserId, AccountType, SecurityStamp, FailedLoginCount, UpdatedUtc)
            OUTPUT inserted.FailedLoginCount INTO @Result (FAILED_LOGIN_COUNT)
            VALUES (@USER_CODE, @ACCOUNT_TYPE, CONVERT(NVARCHAR(64), NEWID()), 1, @NOW_UTC);   -- SecurityStamp is NOT NULL
        END

        COMMIT TRANSACTION;

        SELECT FAILED_LOGIN_COUNT FROM @Result;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        THROW;
    END CATCH
END
GO
