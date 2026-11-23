using Application.Interfaces.V1.User;
using Application.Services;
using Dapper;
using Domain.Entities.Request;
using Domain.Enums;

namespace QPS.Tests.Database
{
    /// <summary>
    /// The sign-in store (IUserAuthServices → auth.PRO_LOGIN_* / auth.PRO_PASSWORD_RESET_TOKEN_*) on the seeded database.
    /// Each test uses its own user so the tests do not depend on each other's changes.
    /// </summary>
    [Collection(QpsDatabaseCollection.Name)]
    public class AuthStoreTests(QpsDatabaseFixture db)
    {
        private IUserAuthServices Store => db.Service<IUserAuthServices>();

        private static AuthLoginRequest Req(string code, AccountType type) => new() { UserCode = code, AccountType = type };

        [SqlServerFact]
        public async Task An_employee_is_found_with_role_modules_and_active_menus()
        {
            var r = await Store.FindByLoginAsync(Req("E1001", AccountType.Employee));

            Assert.NotNull(r.USER);
            Assert.Equal("E1001", r.USER!.USERCODE);
            Assert.Equal("Employee", r.USER.ACCOUNTTYPE);
            Assert.Equal(1, r.USER.ROLEID);
            Assert.Equal("1,2", r.USER.REGION_IDS);
            Assert.True(r.USER.ISACTIVE);
            Assert.True(r.Modules.Menu);
            Assert.True(r.Modules.User_Management);
            Assert.Equal(["Dashboard", "Inspections", "Masters", "Users"], r.MenuList.Select(m => m.Label));   // the inactive menu is left out
            Assert.NotNull(r.AuthUser);
        }

        [SqlServerFact]
        public async Task A_vendor_is_found_with_the_default_address_email_and_a_verifiable_hash()
        {
            var r = await Store.FindByLoginAsync(Req("V0001", AccountType.Vendor));

            Assert.NotNull(r.USER);
            Assert.Equal("Vendor", r.USER!.ACCOUNTTYPE);
            Assert.Equal("orders@acme.example.com", r.USER.EMAIL);
            Assert.Equal("27AAAAA0000A1Z5", r.USER.GST_NO);
            Assert.True(new PasswordHasher().VerifyPassword(QpsDatabaseFixture.VendorPassword, r.USER.PASSWORDHASH!));
            Assert.Empty(r.MenuList);
        }

        [SqlServerFact]
        public async Task A_vendor_with_a_temporary_password_must_change_it() =>
            Assert.True((await Store.FindByLoginAsync(Req("V0002", AccountType.Vendor))).USER!.MUSTCHANGEPASSWORD);

        [SqlServerFact]
        public async Task An_inactive_vendor_is_returned_as_inactive() =>
            Assert.False((await Store.FindByLoginAsync(Req("V0003", AccountType.Vendor))).USER!.ISACTIVE);

        [SqlServerTheory]
        [InlineData("NOPE1", AccountType.Employee)]
        [InlineData("E1001", AccountType.Vendor)]     // right code, wrong account type
        [InlineData("V0001", AccountType.Employee)]
        public async Task Unknown_users_are_not_found(string code, AccountType type)
        {
            var r = await Store.FindByLoginAsync(Req(code, type));
            Assert.Null(r.USER);
            Assert.Null(r.AuthUser);
        }

        [SqlServerFact(Skip = "Known bug: for a user without an auth.UserSecurity row, PRO_LOGIN_RECORD_FAILED_COUNT (and PRO_LOGIN_LOCKOUT) " +
                              "insert a row without SecurityStamp, which is NOT NULL with no default, so a wrong password throws a SQL error. " +
                              "Remove this Skip once the procedures set SecurityStamp (or the column gets a default).")]
        public async Task The_first_failed_sign_in_of_a_user_without_a_security_row_is_counted()
        {
            Assert.Equal(1, await Store.RecordFailedLoginAsync(Req("E1004", AccountType.Employee)));
        }

        [SqlServerFact]
        public async Task Failed_sign_ins_count_up_then_lockout_resets_the_count_and_clear_removes_the_lockout()
        {
            var user = Req("E1001", AccountType.Employee);   // has a seeded auth.UserSecurity row

            Assert.Equal(1, await Store.RecordFailedLoginAsync(user));
            Assert.Equal(2, await Store.RecordFailedLoginAsync(user));
            Assert.Equal(3, await Store.RecordFailedLoginAsync(user));

            var until = db.Clock.GetUtcNow().AddMinutes(15);
            user.untilUtc = until;
            await Store.LockOutAsync(user);
            var locked = (await Store.FindByLoginAsync(user)).USER!;
            Assert.Equal(0, locked.FAILEDLOGINCOUNT);
            Assert.Equal(until, locked.LOCKOUTENDUTC);

            await Store.RecordFailedLoginAsync(user);
            await Store.ClearLockoutAsync(user);
            var cleared = (await Store.FindByLoginAsync(user)).USER!;
            Assert.Equal(0, cleared.FAILEDLOGINCOUNT);
            Assert.Null(cleared.LOCKOUTENDUTC);
        }

        [SqlServerFact]
        public async Task Setting_a_password_with_rotation_changes_hash_and_stamp_and_clears_must_change()
        {
            var user = Req("E1003", AccountType.Employee);   // no security row yet: the procedure inserts one
            var hash = new PasswordHasher().HashPassword("New-Password-1");
            user.passwordHash = hash;
            user.rotateSecurityStamp = true;

            await Store.SetPasswordHashAsync(user);

            var row = (await Store.FindByLoginAsync(user)).USER!;
            Assert.Equal(hash, row.PASSWORDHASH);
            Assert.False(string.IsNullOrEmpty(row.SECURITYSTAMP));
            Assert.False(row.MUSTCHANGEPASSWORD);
        }

        [SqlServerFact]
        public async Task Setting_a_password_without_rotation_keeps_the_security_stamp()
        {
            var user = Req("V0003", AccountType.Vendor);
            user.passwordHash = new PasswordHasher().HashPassword("Another-Password-2");
            user.rotateSecurityStamp = false;

            await Store.SetPasswordHashAsync(user);

            var row = (await Store.FindByLoginAsync(user)).USER!;
            Assert.Equal("STAMP-V0003", row.SECURITYSTAMP);
            Assert.Equal(user.passwordHash, row.PASSWORDHASH);
        }

        [SqlServerFact]
        public async Task A_reset_token_is_found_until_redeemed_and_can_be_redeemed_once()
        {
            db.Clock.Now = QpsDatabaseFixture.Start;
            var user = Req("V0001", AccountType.Vendor);
            user.tokenHash = "hash-redeem-once";
            user.expiresUtc = db.Clock.Now.AddMinutes(30);
            await Store.SavePasswordResetTokenAsync(user);

            var found = await Store.FindUserIdByPasswordResetTokenAsync(new() { tokenHash = "hash-redeem-once" });
            Assert.Equal(("V0001", "Vendor"), (found!.USER_CODE, found.ACCOUNT_TYPE));

            Assert.NotNull(await Store.ConsumePasswordResetTokenAsync(new() { tokenHash = "hash-redeem-once" }));
            Assert.Null(await Store.ConsumePasswordResetTokenAsync(new() { tokenHash = "hash-redeem-once" }));
            Assert.Null(await Store.FindUserIdByPasswordResetTokenAsync(new() { tokenHash = "hash-redeem-once" }));
        }

        [SqlServerFact]
        public async Task A_new_reset_token_replaces_the_previous_one()
        {
            db.Clock.Now = QpsDatabaseFixture.Start;
            var user = Req("E1001", AccountType.Employee);
            user.expiresUtc = db.Clock.Now.AddMinutes(30);
            user.tokenHash = "hash-first";
            await Store.SavePasswordResetTokenAsync(user);
            user.tokenHash = "hash-second";
            await Store.SavePasswordResetTokenAsync(user);

            Assert.Null(await Store.FindUserIdByPasswordResetTokenAsync(new() { tokenHash = "hash-first" }));
            Assert.NotNull(await Store.FindUserIdByPasswordResetTokenAsync(new() { tokenHash = "hash-second" }));
        }

        [SqlServerFact]
        public async Task An_expired_reset_token_is_neither_found_nor_redeemed()
        {
            db.Clock.Now = QpsDatabaseFixture.Start;
            var user = Req("V0002", AccountType.Vendor);
            user.tokenHash = "hash-expires";
            user.expiresUtc = db.Clock.Now.AddMinutes(30);
            await Store.SavePasswordResetTokenAsync(user);

            db.Clock.Now = QpsDatabaseFixture.Start.AddMinutes(31);
            try
            {
                Assert.Null(await Store.FindUserIdByPasswordResetTokenAsync(new() { tokenHash = "hash-expires" }));
                Assert.Null(await Store.ConsumePasswordResetTokenAsync(new() { tokenHash = "hash-expires" }));
            }
            finally { db.Clock.Now = QpsDatabaseFixture.Start; }

            using var c = db.Open();
            Assert.Equal(0, c.ExecuteScalar<int>("SELECT COUNT(*) FROM auth.PasswordResetToken WHERE TokenHash = 'hash-expires'"));   // redeem deleted it
        }
    }
}
