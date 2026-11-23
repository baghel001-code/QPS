using Application.Services;

namespace QPS.Tests.Security
{
    /// <summary>The vendor password hash (PBKDF2-SHA256, 100 000 iterations, 16-byte salt) and the temporary passwords it e-mails.</summary>
    public class PasswordHasherTests
    {
        private readonly PasswordHasher _hasher = new();

        [Fact]
        public void A_hash_verifies_the_right_password_and_not_a_wrong_one()
        {
            var hash = _hasher.HashPassword("Blue-Harbour-42");
            Assert.True(_hasher.VerifyPassword("Blue-Harbour-42", hash));
            Assert.False(_hasher.VerifyPassword("blue-harbour-42", hash));
            Assert.False(_hasher.VerifyPassword("", hash));
        }

        [Fact]
        public void The_hash_is_48_bytes_of_salt_and_key_and_is_salted()
        {
            var a = _hasher.HashPassword("same-password");
            var b = _hasher.HashPassword("same-password");

            Assert.Equal(48, Convert.FromBase64String(a).Length);
            Assert.NotEqual(a, b);                       // a new salt every time
            Assert.True(_hasher.VerifyPassword("same-password", b));
        }

        [Theory]
        [InlineData(8)]
        [InlineData(12)]
        [InlineData(20)]
        public void Temporary_passwords_have_the_length_asked_and_one_of_each_character_group(int length)
        {
            var p = _hasher.GenerateRandomPassword(length);

            Assert.Equal(length, p.Length);
            Assert.Contains(p, char.IsUpper);
            Assert.Contains(p, char.IsLower);
            Assert.Contains(p, char.IsDigit);
            Assert.Contains(p, c => "@#$".Contains(c));
        }

        [Fact]
        public void Temporary_passwords_are_not_repeated()
        {
            var seen = Enumerable.Range(0, 200).Select(_ => _hasher.GenerateRandomPassword(12)).ToHashSet();
            Assert.Equal(200, seen.Count);
        }

        [Fact]
        public void Temporary_passwords_shorter_than_8_are_refused() =>
            Assert.Throws<ArgumentException>(() => _hasher.GenerateRandomPassword(7));
    }
}
