using Application.Interfaces.V1.User;
using Domain.Entities.Request;

namespace QPS.Tests.Database
{
    /// <summary>One session per user (ISessionTokenService → PRO_USER_SESSIONS / _VALIDATE / _CLEAR on USER_ACTIVE_SESSION).</summary>
    [Collection(QpsDatabaseCollection.Name)]
    public class SessionStoreTests(QpsDatabaseFixture db)
    {
        private ISessionTokenService Sessions => db.Service<ISessionTokenService>();

        private static string NewToken() => Guid.NewGuid().ToString("N");   // CHAR(32), as the portal issues it

        [SqlServerFact]
        public async Task An_issued_session_is_valid_and_another_token_is_not()
        {
            var token = NewToken();
            await Sessions.IssueNewSessionAsync(new("Employee", "E1002", 2, token, "test pc"));

            var ok = await Sessions.IsSessionValidAsync(new("Employee", "E1002", 2, token));
            Assert.True(ok.IsValid);

            var other = await Sessions.IsSessionValidAsync(new("Employee", "E1002", 2, NewToken()));
            Assert.False(other.IsValid);
            Assert.Equal(token, other.SessionToken);
        }

        [SqlServerFact]
        public async Task Signing_in_again_replaces_the_previous_session()
        {
            var first = NewToken();
            var second = NewToken();
            await Sessions.IssueNewSessionAsync(new("Employee", "E1003", 3, first, "pc"));
            await Sessions.IssueNewSessionAsync(new("Employee", "E1003", 3, second, "laptop"));

            Assert.False((await Sessions.IsSessionValidAsync(new("Employee", "E1003", 3, first))).IsValid);
            Assert.True((await Sessions.IsSessionValidAsync(new("Employee", "E1003", 3, second))).IsValid);
        }

        [SqlServerFact]
        public async Task A_cleared_session_is_no_longer_valid()
        {
            var token = NewToken();
            await Sessions.IssueNewSessionAsync(new("Vendor", "V0002", 2, token));
            await Sessions.ClearSessionAsync(new("Vendor", "V0002", 2));

            var r = await Sessions.IsSessionValidAsync(new("Vendor", "V0002", 2, token));
            Assert.False(r.IsValid);
            Assert.Equal("", r.SessionToken);
        }

        [SqlServerFact]
        public async Task A_user_who_never_signed_in_has_no_valid_session()
        {
            var r = await Sessions.IsSessionValidAsync(new("Employee", "E1005", 5, NewToken()));
            Assert.False(r.IsValid);
            Assert.Equal("", r.SessionToken);
        }

        [SqlServerFact]
        public async Task An_employee_and_a_vendor_with_the_same_numeric_id_have_separate_sessions()
        {
            var employee = NewToken();
            var vendor = NewToken();
            await Sessions.IssueNewSessionAsync(new("Employee", "E1001", 1, employee));
            await Sessions.IssueNewSessionAsync(new("Vendor", "V0001", 1, vendor));

            Assert.True((await Sessions.IsSessionValidAsync(new("Employee", "E1001", 1, employee))).IsValid);
            Assert.True((await Sessions.IsSessionValidAsync(new("Vendor", "V0001", 1, vendor))).IsValid);
        }
    }
}
