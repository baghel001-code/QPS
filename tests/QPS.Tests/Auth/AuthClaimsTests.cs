using System.Security.Claims;
using Admin.Auth;
using Domain.Enums;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using QPS.Tests.Platform;

namespace QPS.Tests.Auth
{
    /// <summary>
    /// The cookie principal built at sign-in (AuthClaims), what the portal reads back from it (QpsRoles, account type),
    /// the absolute session lifetime and where the user lands (LandingPageResolver).
    /// </summary>
    public class AuthClaimsTests
    {
        private static readonly DateTimeOffset SignedIn = new(2026, 11, 23, 9, 0, 0, TimeSpan.Zero);

        [Fact]
        public void An_employee_gets_their_role_ids_as_role_claims()
        {
            var p = AuthClaims.CreatePrincipal(Users.Employee(roles: [QpsRoles.QAM, QpsRoles.QAAdmin]), SignedIn);

            Assert.True(p.Identity!.IsAuthenticated);
            Assert.Equal("E1001", p.FindFirstValue(ClaimTypes.NameIdentifier));
            Assert.True(QpsRoles.Has(p, QpsRoles.QAM));
            Assert.True(QpsRoles.Has(p, QpsRoles.QAAdmin));
            Assert.False(QpsRoles.Has(p, QpsRoles.Vendor));
            Assert.Equal(AccountType.Employee, p.GetAccountType());
            Assert.True(p.IsEmployee());
            Assert.False(QpsRoles.IsVendor(p));
        }

        [Fact]
        public void A_vendor_gets_only_the_vendor_role()
        {
            var p = AuthClaims.CreatePrincipal(Users.Vendor(), SignedIn);

            Assert.True(QpsRoles.IsVendor(p));
            Assert.True(p.IsVendor());
            Assert.Equal(QpsRoles.Vendor, QpsRoles.PrimaryRole(p));
            Assert.Equal("Vendor", QpsRoles.Describe(p));
            Assert.Single(p.FindAll(ClaimTypes.Role));
        }

        [Fact]
        public void The_primary_role_follows_the_dashboard_priority()
        {
            var roles = QpsRoles.DashboardPriority.Take(2).Reverse().ToArray();   // second-highest listed first
            var p = AuthClaims.CreatePrincipal(Users.Employee(roles: roles), SignedIn);
            Assert.Equal(QpsRoles.DashboardPriority[0], QpsRoles.PrimaryRole(p));
        }

        [Fact]
        public void An_employee_without_roles_is_described_as_employee()
        {
            var p = AuthClaims.CreatePrincipal(Users.Employee(), SignedIn);
            Assert.Null(QpsRoles.PrimaryRole(p));
            Assert.Equal("Employee", QpsRoles.Describe(p));
        }

        [Fact]
        public void Each_sign_in_gets_its_own_session_id_unless_one_is_given()
        {
            var user = Users.Employee();
            var a = AuthClaims.CreatePrincipal(user, SignedIn).FindFirstValue(AuthClaimTypes.SessionId);
            var b = AuthClaims.CreatePrincipal(user, SignedIn).FindFirstValue(AuthClaimTypes.SessionId);
            Assert.NotEqual(a, b);
            Assert.Equal("fixed", AuthClaims.CreatePrincipal(user, SignedIn, "fixed").FindFirstValue(AuthClaimTypes.SessionId));
        }

        [Fact]
        public void The_session_is_valid_until_the_absolute_lifetime_and_not_after()
        {
            var settings = new AuthSettings { AbsoluteLifetimeHours = 12 };
            var p = AuthClaims.CreatePrincipal(Users.Employee(), SignedIn);

            Assert.True(AuthClaims.IsWithinAbsoluteLifetime(p, settings, SignedIn.AddHours(11).AddMinutes(59)));
            Assert.False(AuthClaims.IsWithinAbsoluteLifetime(p, settings, SignedIn.AddHours(12)));
        }

        [Fact]
        public void A_principal_without_a_sign_in_time_is_never_within_the_lifetime() =>
            Assert.False(AuthClaims.IsWithinAbsoluteLifetime(new ClaimsPrincipal(new ClaimsIdentity()), new AuthSettings(), SignedIn));

        [Fact]
        public void Users_land_on_home_or_on_their_return_url()
        {
            var resolver = Resolver(new AuthSettings());
            var p = AuthClaims.CreatePrincipal(Users.Employee(), SignedIn);

            Assert.Equal(LandingPageResolver.Home, resolver.For(p));
            Assert.Equal("/qps/inspections", resolver.AfterSignIn(p, "/qps/inspections"));
        }

        [Fact]
        public void Users_who_must_change_their_password_go_there_first()
        {
            var resolver = Resolver(new AuthSettings());
            var p = AuthClaims.CreatePrincipal(Users.Vendor() with { MustChangePassword = true }, SignedIn);

            Assert.Equal("/changepassword", resolver.For(p));
            Assert.Equal("/changepassword", resolver.AfterSignIn(p, "/qps/inspections"));
        }

        [Fact]
        public void A_non_local_change_password_setting_falls_back_to_home()
        {
            var settings = new AuthSettings { LandingPages = { ChangePassword = "https://evil.example.com/" } };
            var p = AuthClaims.CreatePrincipal(Users.Vendor() with { MustChangePassword = true }, SignedIn);
            Assert.Equal(LandingPageResolver.Home, Resolver(settings).For(p));
        }

        private static LandingPageResolver Resolver(AuthSettings settings) =>
            new(Options.Create(settings), NullLogger<LandingPageResolver>.Instance);
    }
}
