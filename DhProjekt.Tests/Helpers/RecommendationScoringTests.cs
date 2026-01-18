using System.Collections.Generic;
using DhProjekt.Database;
using DhProjekt.Helpers;
using Xunit;

namespace DhProjekt.Tests.Helpers
{
    public class RecommendationScoringTests
    {
        [Fact]
        public void Score_GenreMatch_Adds2()
        {
            var media = new MediaItem { Genre = "Sci-Fi" };
            var genres = new HashSet<string>(System.StringComparer.OrdinalIgnoreCase) { "Sci-Fi" };

            var (score, _) = RecommendationScoring.Score(media, genres, null, null);
            Assert.Equal(2, score);
        }

        [Fact]
        public void Score_MediaTypeMatch_Adds1()
        {
            var media = new MediaItem { MediaType = "movie" };
            var genres = new HashSet<string>(System.StringComparer.OrdinalIgnoreCase);

            var (score, _) = RecommendationScoring.Score(media, genres, "movie", null);
            Assert.Equal(1, score);
        }

        [Fact]
        public void Score_AgeRestrictionMatch_Adds1()
        {
            var media = new MediaItem { AgeRestriction = 12 };
            var genres = new HashSet<string>(System.StringComparer.OrdinalIgnoreCase);

            var (score, _) = RecommendationScoring.Score(media, genres, null, 16);
            Assert.Equal(1, score);
        }

        [Fact]
        public void Score_AllThreeRules_Adds4()
        {
            var media = new MediaItem { Genre = "Sci-Fi", MediaType = "movie", AgeRestriction = 12 };
            var genres = new HashSet<string>(System.StringComparer.OrdinalIgnoreCase) { "Sci-Fi" };

            var (score, reason) = RecommendationScoring.Score(media, genres, "movie", 16);

            Assert.Equal(4, score);
            Assert.Contains("Genre", reason);
            Assert.Contains("MediaType", reason);
            Assert.Contains("AgeRestriction", reason);
        }
    }
}
