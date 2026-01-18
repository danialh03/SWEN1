using System;
using System.Collections.Generic;
using DhProjekt.Database;

namespace DhProjekt.Helpers
{
    public static class RecommendationScoring
    {
        // Gleiche Logik wie in RecommendationRepository, aber ohne DB
        public static (int score, string reason) Score(
            MediaItem media,
            HashSet<string> favoriteGenres,
            string? favoriteMediaType,
            int? preferredMaxAgeRestriction)
        {
            int score = 0;
            var reasons = new List<string>();

            if (!string.IsNullOrWhiteSpace(media.Genre) && favoriteGenres.Contains(media.Genre))
            {
                score += 2;
                reasons.Add("Genre passt (+2)");
            }

            if (!string.IsNullOrWhiteSpace(media.MediaType) &&
                !string.IsNullOrWhiteSpace(favoriteMediaType) &&
                string.Equals(media.MediaType, favoriteMediaType, StringComparison.OrdinalIgnoreCase))
            {
                score += 1;
                reasons.Add("MediaType passt (+1)");
            }

            if (preferredMaxAgeRestriction != null &&
                media.AgeRestriction != null &&
                media.AgeRestriction <= preferredMaxAgeRestriction)
            {
                score += 1;
                reasons.Add("AgeRestriction passt (+1)");
            }

            return (score, string.Join("; ", reasons));
        }
    }
}
