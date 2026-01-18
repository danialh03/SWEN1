using DhProjekt.Helpers;
using Xunit;

namespace DhProjekt.Tests.Auth
{
    public class AuthHeaderHelperTests
    {
        [Fact]
        public void ExtractBearerToken_ValidAuthorization_ReturnsToken()
        {
            var t = AuthHeaderHelper.ExtractBearerToken("Bearer abc123");
            Assert.Equal("abc123", t);
        }

        [Fact]
        public void ExtractBearerToken_IgnoresCase_ReturnsToken()
        {
            var t = AuthHeaderHelper.ExtractBearerToken("bearer XYZ");
            Assert.Equal("XYZ", t);
        }

        [Fact]
        public void ExtractBearerToken_InvalidPrefix_ReturnsNull()
        {
            var t = AuthHeaderHelper.ExtractBearerToken("Token abc");
            Assert.Null(t);
        }

        [Fact]
        public void ExtractBearerToken_EmptyToken_ReturnsNull()
        {
            var t = AuthHeaderHelper.ExtractBearerToken("Bearer   ");
            Assert.Null(t);
        }

        [Fact]
        public void TryGetBearerToken_PrefersAuthenticationHeader()
        {
            var t = AuthHeaderHelper.TryGetBearerToken("Bearer A", "Bearer B");
            Assert.Equal("B", t);
        }

        [Fact]
        public void TryGetBearerToken_FallbackToAuthorization()
        {
            var t = AuthHeaderHelper.TryGetBearerToken("Bearer A", null);
            Assert.Equal("A", t);
        }
    }
}
