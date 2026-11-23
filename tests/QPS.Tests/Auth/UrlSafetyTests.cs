using Admin.Auth;

namespace QPS.Tests.Auth
{
    /// <summary>ReturnUrl handling after sign-in: only local paths are followed, everything else goes to /nopage.</summary>
    public class UrlSafetyTests
    {
        [Theory]
        [InlineData(null, "/")]
        [InlineData("", "/")]
        [InlineData("   ", "/")]
        [InlineData("/qps/dashboard", "/qps/dashboard")]
        [InlineData("/Inspections?id=5", "/Inspections?id=5")]
        public void Local_paths_are_kept_and_empty_goes_home(string? url, string expected) =>
            Assert.Equal(expected, UrlSafety.LocalOrRoot(url));

        [Theory]
        [InlineData("https://evil.example.com/")]
        [InlineData("//evil.example.com")]
        [InlineData("/\\evil.example.com")]
        [InlineData("javascript:alert(1)")]
        [InlineData("/ok\r\nSet-Cookie:x=1")]
        public void External_or_malformed_urls_are_rejected(string url) =>
            Assert.Equal(UrlSafety.RejectedPath, UrlSafety.LocalOrRoot(url));

        [Theory]
        [InlineData("/Account/Login")]
        [InlineData("/account/logout?x=1")]
        public void Returning_to_the_login_or_logout_page_goes_home_instead_of_looping(string url) =>
            Assert.Equal("/", UrlSafety.LocalOrRoot(url));
    }
}
