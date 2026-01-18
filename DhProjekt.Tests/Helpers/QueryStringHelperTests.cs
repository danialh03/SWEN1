using DhProjekt.Helpers;
using Xunit;

namespace DhProjekt.Tests.Helpers
{
    public class QueryStringHelperTests
    {
        [Fact]
        public void Parse_Empty_ReturnsEmptyDictionary()
        {
            var d = QueryStringHelper.Parse(null);
            Assert.Equal(0, d.Count);
        }

        [Fact]
        public void Parse_SimpleKeyValue()
        {
            var d = QueryStringHelper.Parse("?a=1&b=hello");
            Assert.Equal("1", d["a"]);
            Assert.Equal("hello", d["b"]);
        }

        [Fact]
        public void Parse_MissingValue_BecomesEmptyString()
        {
            var d = QueryStringHelper.Parse("?a=");
            Assert.Equal("", d["a"]);
        }

        [Fact]
        public void Parse_NoEquals_BecomesEmptyValue()
        {
            var d = QueryStringHelper.Parse("?a");
            Assert.Equal("", d["a"]);
        }

        [Fact]
        public void Parse_DecodesUrlEncoding()
        {
            var d = QueryStringHelper.Parse("?q=hello%20world");
            Assert.Equal("hello world", d["q"]);
        }

        [Fact]
        public void Parse_CaseInsensitiveKeys_LastWins()
        {
            var d = QueryStringHelper.Parse("?A=1&a=2");
            Assert.Equal("2", d["a"]);
        }
    }
}
