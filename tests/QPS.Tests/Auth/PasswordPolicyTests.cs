using Admin.Auth;
using QPS.Tests.Platform;

namespace QPS.Tests.Auth
{
    /// <summary>The password rules used by reset / change password (length, variety, block-list, no username).</summary>
    public class PasswordPolicyTests
    {
        private static readonly Domain.Records.AuthUser User = Users.Vendor(userName: "acmetextiles");

        [Fact]
        public void A_long_varied_password_passes() =>
            Assert.Empty(PasswordPolicy.Validate("Blue-Harbour-42-Kite", User, minLength: 10));

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        public void An_empty_password_is_rejected_with_one_message(string? password) =>
            Assert.Equal(["Enter a password."], PasswordPolicy.Validate(password, User, 10));

        [Fact]
        public void Too_short_is_rejected() =>
            Assert.Contains("Use at least 10 characters.", PasswordPolicy.Validate("Ab3$xyz", User, 10));

        [Fact]
        public void Too_long_is_rejected() =>
            Assert.Contains($"Use at most {PasswordPolicy.MaxLength} characters.",
                PasswordPolicy.Validate(new string('a', 64) + new string('b', 64) + "cd!", User, 10));

        [Fact]
        public void Repeated_characters_are_rejected() =>
            Assert.Contains("Use more varied characters.", PasswordPolicy.Validate("aaaaaaaaaabbb", User, 10));

        [Theory]
        [InlineData("password123")]
        [InlineData("Welcome@123")]
        [InlineData("CHANGEME123")]
        public void Common_passwords_are_rejected_case_insensitively(string password) =>
            Assert.Contains("This password is too common.", PasswordPolicy.Validate(password, User, 10));

        [Theory]
        [InlineData("my-AcmeTextiles-2026")]   // user name
        [InlineData("x-acmetextiles-pw-99")]   // e-mail local part (same as the user name here)
        public void Passwords_containing_the_username_or_email_are_rejected(string password) =>
            Assert.Contains("Don't include your username or e-mail in the password.", PasswordPolicy.Validate(password, User, 10));
    }
}
