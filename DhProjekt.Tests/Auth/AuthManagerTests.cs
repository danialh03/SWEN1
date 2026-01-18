using DhProjekt.Auth;
using Xunit;

namespace DhProjekt.Tests.Auth
{
    public class AuthManagerTests
    {
        [Fact]
        public void TokenLifetime_Is12Hours()
        {
            Assert.Equal(12, AuthManager.TokenLifetime.TotalHours);
        }

        [Fact]
        public void GenerateToken_Is32Chars_NoDashes()
        {
            var token = AuthManager.GenerateToken();

            Assert.Equal(32, token.Length);
            Assert.DoesNotContain("-", token);
        }
    }
}
