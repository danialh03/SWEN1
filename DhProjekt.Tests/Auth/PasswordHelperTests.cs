using DhProjekt.Auth;
using Xunit;

namespace DhProjekt.Tests.Auth
{
    public class PasswordHelperTests
    {
        [Fact]
        public void HashPassword_NotNullOrEmpty()
        {
            var hash = PasswordHelper.HashPassword("1234");
            Assert.False(string.IsNullOrWhiteSpace(hash));
        }

        [Fact]
        public void HashPassword_StartsWithPbkdf2Prefix()
        {
            var hash = PasswordHelper.HashPassword("test");
            Assert.StartsWith("PBKDF2$", hash);
        }

        [Fact]
        public void HashPassword_SamePassword_ProducesDifferentHashesBecauseOfSalt()
        {
            var h1 = PasswordHelper.HashPassword("abc");
            var h2 = PasswordHelper.HashPassword("abc");

            Assert.NotEqual(h1, h2);
        }

        [Fact]
        public void VerifyPassword_CorrectPassword_ReturnsTrue()
        {
            var stored = PasswordHelper.HashPassword("secret");
            Assert.True(PasswordHelper.VerifyPassword("secret", stored));
        }

        [Fact]
        public void VerifyPassword_WrongPassword_ReturnsFalse()
        {
            var stored = PasswordHelper.HashPassword("secret");
            Assert.False(PasswordHelper.VerifyPassword("wrong", stored));
        }

        [Fact]
        public void VerifyPassword_InvalidStoredHash_ReturnsFalse()
        {
            Assert.False(PasswordHelper.VerifyPassword("secret", "not-a-valid-hash"));
        }


        [Fact]
        public void VerifyPassword_LegacySha256HexHash_StillWorks()
        {
            // Legacy SHA256 Hex von "test" (uppercase)
            using var sha = System.Security.Cryptography.SHA256.Create();
            var bytes = System.Text.Encoding.UTF8.GetBytes("test");
            var legacy = System.Convert.ToHexString(sha.ComputeHash(bytes));

            Assert.True(PasswordHelper.VerifyPassword("test", legacy));
            Assert.False(PasswordHelper.VerifyPassword("wrong", legacy));
        }
    }
}
