using System;
using System.Collections.Generic;
using Npgsql;

namespace DhProjekt.Database
{
    /// Liest Daten aus DB und baut Recommendations.
    public class RecommendationRepository
    {
        private readonly DatabaseConnection _db;

        public RecommendationRepository(DatabaseConnection db)
        {
            _db = db;
        }

        public List<(MediaItem media, int score, string reason)> GetRecommendations(int userId, int limit)
        {
            if (limit <= 0) limit = 10;
            if (limit > 50) limit = 50;

            // 1) Preferences aus "hoch bewerteten" Medien ziehen (stars >= 4)
            var favoriteGenres = GetFavoriteGenres(userId);
            var favoriteMediaType = GetFavoriteMediaType(userId);
            var preferredMaxAge = GetPreferredMaxAgeRestriction(userId);

            // 2) Kandidaten = Medien, die der User noch NICHT bewertet hat
            var candidates = GetNotYetRatedMedia(userId);

            // 3) Score berechnen 
            var scored = new List<(MediaItem media, int score, string reason)>();

            foreach (var m in candidates)
            {
                int score = 0;
                var reasons = new List<string>();

                if (!string.IsNullOrWhiteSpace(m.Genre) && favoriteGenres.Contains(m.Genre))
                {
                    score += 2;
                    reasons.Add("Genre passt zu deinen hoch bewerteten Medien (+2)");
                }

                if (!string.IsNullOrWhiteSpace(m.MediaType) && !string.IsNullOrWhiteSpace(favoriteMediaType) &&
                    string.Equals(m.MediaType, favoriteMediaType, StringComparison.OrdinalIgnoreCase))
                {
                    score += 1;
                    reasons.Add("MediaType passt zu deinem häufigsten Lieblings-Typ (+1)");
                }

                if (preferredMaxAge != null && m.AgeRestriction != null && m.AgeRestriction <= preferredMaxAge)
                {
                    score += 1;
                    reasons.Add("AgeRestriction passt zu deinen bisherigen Favoriten (+1)");
                }

                // Nur empfehlen, wenn mindestens 1 Regel passt.
                if (score > 0)
                {
                    scored.Add((m, score, string.Join("; ", reasons)));
                }
            }

            // 4) Sortieren: Score DESC, dann ReleaseYear DESC, dann Id DESC
            scored.Sort((a, b) =>
            {
                var c = b.score.CompareTo(a.score);
                if (c != 0) return c;

                int ay = a.media.ReleaseYear ?? -1;
                int by = b.media.ReleaseYear ?? -1;
                c = by.CompareTo(ay);
                if (c != 0) return c;

                return b.media.Id.CompareTo(a.media.Id);
            });

            // 5) Top N zurückgeben
            if (scored.Count > limit)
                scored = scored.GetRange(0, limit);

            return scored;
        }

        private HashSet<string> GetFavoriteGenres(int userId)
        {
            using var conn = _db.OpenConnection();

            const string sql = @"
                SELECT m.genre, COUNT(*) AS cnt
                FROM ratings r
                JOIN media m ON m.id = r.media_id
                WHERE r.user_id = @user_id
                  AND r.stars >= 4
                  AND m.genre IS NOT NULL
                GROUP BY m.genre
                ORDER BY cnt DESC";

            using var cmd = new NpgsqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("user_id", userId);

            using var reader = cmd.ExecuteReader();
            var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            while (reader.Read())
            {
                var genre = reader.IsDBNull(0) ? null : reader.GetString(0);
                if (!string.IsNullOrWhiteSpace(genre))
                    set.Add(genre);
            }

            return set;
        }

        private string? GetFavoriteMediaType(int userId)
        {
            using var conn = _db.OpenConnection();

            const string sql = @"
                SELECT m.media_type, COUNT(*) AS cnt
                FROM ratings r
                JOIN media m ON m.id = r.media_id
                WHERE r.user_id = @user_id
                  AND r.stars >= 4
                  AND m.media_type IS NOT NULL
                GROUP BY m.media_type
                ORDER BY cnt DESC
                LIMIT 1";

            using var cmd = new NpgsqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("user_id", userId);

            var result = cmd.ExecuteScalar();
            return result == null ? null : result.ToString();
        }

        private int? GetPreferredMaxAgeRestriction(int userId)
        {
            using var conn = _db.OpenConnection();

            const string sql = @"
                SELECT MAX(m.age_restriction)
                FROM ratings r
                JOIN media m ON m.id = r.media_id
                WHERE r.user_id = @user_id
                  AND r.stars >= 4
                  AND m.age_restriction IS NOT NULL";

            using var cmd = new NpgsqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("user_id", userId);

            var result = cmd.ExecuteScalar();
            if (result == null || result == DBNull.Value)
                return null;

            return Convert.ToInt32(result);
        }

        private List<MediaItem> GetNotYetRatedMedia(int userId)
        {
            using var conn = _db.OpenConnection();

            const string sql = @"
                SELECT id, title, description, media_type, release_year, genre, age_restriction, created_by
                FROM media
                WHERE id NOT IN (
                    SELECT media_id FROM ratings WHERE user_id = @user_id
                )
                ORDER BY id";

            using var cmd = new NpgsqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("user_id", userId);

            using var reader = cmd.ExecuteReader();
            var result = new List<MediaItem>();

            while (reader.Read())
            {
                result.Add(new MediaItem
                {
                    Id = reader.GetInt32(0),
                    Title = reader.GetString(1),
                    Description = reader.IsDBNull(2) ? null : reader.GetString(2),
                    MediaType = reader.IsDBNull(3) ? null : reader.GetString(3),
                    ReleaseYear = reader.IsDBNull(4) ? null : reader.GetInt32(4),
                    Genre = reader.IsDBNull(5) ? null : reader.GetString(5),
                    AgeRestriction = reader.IsDBNull(6) ? null : reader.GetInt32(6),
                    CreatedBy = reader.GetInt32(7)
                });
            }

            return result;
        }
    }
}
