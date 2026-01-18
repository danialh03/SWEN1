using DhProjekt.Helpers;
using Xunit;

namespace DhProjekt.Tests.Helpers
{
    public class MediaSortHelperTests
    {
        [Fact]
        public void GetOrderByExpression_Title()
        {
            Assert.Equal("m.title", MediaSortHelper.GetOrderByExpression("title"));
        }

        [Fact]
        public void GetOrderByExpression_Score()
        {
            Assert.Equal("COALESCE(r.avg_score, 0)", MediaSortHelper.GetOrderByExpression("score"));
        }

        [Fact]
        public void GetOrderByExpression_Default()
        {
            Assert.Equal("m.id", MediaSortHelper.GetOrderByExpression("unknown"));
        }

        [Fact]
        public void GetOrderDirection_DefaultIsAsc()
        {
            Assert.Equal("ASC", MediaSortHelper.GetOrderDirection(null));
        }
    }
}
