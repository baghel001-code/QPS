using Admin.Auth;
using Domain.Enums;
using QPS.Tests.Platform;

namespace QPS.Tests.Auth
{
    /// <summary>The single-use, 60-second ticket that hands a verified sign-in from the Blazor circuit to /account/complete-login.</summary>
    public class LoginTicketStoreTests
    {
        private readonly FakeClock _clock = new();
        private readonly LoginTicketStore _store;

        public LoginTicketStoreTests() => _store = new LoginTicketStore(_clock);

        [Fact]
        public void A_ticket_is_64_hex_characters_and_redeems_to_the_signed_in_user()
        {
            var ticket = _store.Issue("E1001", AccountType.Employee, rememberMe: true);
            Assert.Matches("^[0-9A-F]{64}$", ticket);

            var t = _store.Redeem(ticket);
            Assert.NotNull(t);
            Assert.Equal("E1001", t!.UserCode);
            Assert.Equal(AccountType.Employee, t.AccountType);
            Assert.True(t.RememberMe);
        }

        [Fact]
        public void A_ticket_can_be_redeemed_only_once()
        {
            var ticket = _store.Issue("V2001", AccountType.Vendor, rememberMe: false);
            Assert.NotNull(_store.Redeem(ticket));
            Assert.Null(_store.Redeem(ticket));
        }

        [Fact]
        public void A_ticket_expires_after_60_seconds()
        {
            var ticket = _store.Issue("E1001", AccountType.Employee, false);
            _clock.Advance(TimeSpan.FromSeconds(61));
            Assert.Null(_store.Redeem(ticket));
        }

        [Fact]
        public void A_ticket_still_works_just_before_it_expires()
        {
            var ticket = _store.Issue("E1001", AccountType.Employee, false);
            _clock.Advance(TimeSpan.FromSeconds(59));
            Assert.NotNull(_store.Redeem(ticket));
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("ABC")]
        public void Missing_or_malformed_tickets_are_refused(string? ticket) =>
            Assert.Null(_store.Redeem(ticket));

        [Fact]
        public void Each_ticket_is_different()
        {
            var a = _store.Issue("E1001", AccountType.Employee, false);
            var b = _store.Issue("E1001", AccountType.Employee, false);
            Assert.NotEqual(a, b);
        }
    }
}
