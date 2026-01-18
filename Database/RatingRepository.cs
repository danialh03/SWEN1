using System;
using System.Collections.Generic;
using Npgsql;

namespace DhProjekt.Database
{
    public class RatingRepository
    {
        private readonly DatabaseConnection _db;

        public RatingRepository(DatabaseConnection db)
        {
            _db = db;
        }

        // 1) Rating erstellen (pro User+Media nur 1x wegen UNIQUE)
        public Rating? Create(int mediaId, int userId, int stars, string? comment)
        {
            using var conn = _db.OpenConnection();

            const string sql = @"
                INSERT INTO ratings (media_id, user_id, stars, comment, comment_confirmed, created_at, updated_at)
                VALUES (@media_id, @user_id, @stars, @comment, FALSE, NOW(), NOW())
                RETURNING id, media_id, user_id, stars, comment, comment_confirmed, created_at, updated_at";

            using var cmd = new NpgsqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("media_id", mediaId);
            cmd.Parameters.AddWithValue("user_id", userId);
            cmd.Parameters.AddWithValue("stars", stars);
            cmd.Parameters.AddWithValue("comment", (object?)comment ?? DBNull.Value);

            try
            {
                using var reader = cmd.ExecuteReader();
                if (!reader.Read()) return null;
                return ReadRating(reader);
            }
            catch (PostgresException ex) when (ex.SqlState == "23505")
            {
                return null;
            }
        }

        // 2) Alle Ratings zu einem Media (inkl. LikeCount)
        public List<(Rating rating, int likeCount)> GetByMedia(int mediaId)
        {
            using var conn = _db.OpenConnection();

            const string sql = @"
                SELECT r.id, r.media_id, r.user_id, r.stars, r.comment, r.comment_confirmed, r.created_at, r.updated_at,
                       COALESCE(l.cnt, 0) AS like_count
                FROM ratings r
                LEFT JOIN (
                    SELECT rating_id, COUNT(*) AS cnt
                    FROM rating_likes
                    GROUP BY rating_id
                ) l ON l.rating_id = r.id
                WHERE r.media_id = @media_id
                ORDER BY r.created_at DESC";

            using var cmd = new NpgsqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("media_id", mediaId);

            using var reader = cmd.ExecuteReader();
            var result = new List<(Rating, int)>();

            while (reader.Read())
            {
                var rating = ReadRating(reader);
                var likeCount = reader.GetInt32(8);
                result.Add((rating, likeCount));
            }

            return result;
        }

        // 3) Rating by id
        public Rating? GetById(int ratingId)
        {
            using var conn = _db.OpenConnection();

            const string sql = @"
                SELECT id, media_id, user_id, stars, comment, comment_confirmed, created_at, updated_at
                FROM ratings
                WHERE id = @id";

            using var cmd = new NpgsqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("id", ratingId);

            using var reader = cmd.ExecuteReader();
            if (!reader.Read()) return null;

            return ReadRating(reader);
        }

        // 4) Update (nur Owner)
        public bool Update(int ratingId, int userId, int stars, string? comment)
        {
            using var conn = _db.OpenConnection();

            const string sql = @"
                UPDATE ratings
                SET stars = @stars,
                    comment = @comment,
                    updated_at = NOW()
                WHERE id = @id AND user_id = @user_id";

            using var cmd = new NpgsqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("id", ratingId);
            cmd.Parameters.AddWithValue("user_id", userId);
            cmd.Parameters.AddWithValue("stars", stars);
            cmd.Parameters.AddWithValue("comment", (object?)comment ?? DBNull.Value);

            return cmd.ExecuteNonQuery() > 0;
        }

        // 5) Delete (nur Owner)
        public bool Delete(int ratingId, int userId)
        {
            using var conn = _db.OpenConnection();

            const string sql = @"DELETE FROM ratings WHERE id = @id AND user_id = @user_id";

            using var cmd = new NpgsqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("id", ratingId);
            cmd.Parameters.AddWithValue("user_id", userId);

            return cmd.ExecuteNonQuery() > 0;
        }

        // 6) Confirm Comment (nur Owner)
        public bool ConfirmComment(int ratingId, int userId)
        {
            using var conn = _db.OpenConnection();

            const string sql = @"
                UPDATE ratings
                SET comment_confirmed = TRUE,
                    updated_at = NOW()
                WHERE id = @id AND user_id = @user_id";

            using var cmd = new NpgsqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("id", ratingId);
            cmd.Parameters.AddWithValue("user_id", userId);

            return cmd.ExecuteNonQuery() > 0;
        }

        // 7) Like hinzufügen
        public bool Like(int ratingId, int userId)
        {
            using var conn = _db.OpenConnection();

            const string sql = @"
                INSERT INTO rating_likes (rating_id, user_id, created_at)
                VALUES (@rating_id, @user_id, NOW())";

            using var cmd = new NpgsqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("rating_id", ratingId);
            cmd.Parameters.AddWithValue("user_id", userId);

            try
            {
                return cmd.ExecuteNonQuery() > 0;
            }
            catch (PostgresException ex) when (ex.SqlState == "23505")
            {
                return false;
            }
        }

        // 8) Like entfernen
        public bool Unlike(int ratingId, int userId)
        {
            using var conn = _db.OpenConnection();

            const string sql = @"DELETE FROM rating_likes WHERE rating_id = @rating_id AND user_id = @user_id";

            using var cmd = new NpgsqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("rating_id", ratingId);
            cmd.Parameters.AddWithValue("user_id", userId);

            return cmd.ExecuteNonQuery() > 0;
        }

        // 9) AverageScore + RatingsCount pro Media
        public (double? average, int count) GetMediaStats(int mediaId)
        {
            using var conn = _db.OpenConnection();

            const string sql = @"
                SELECT AVG(stars)::float8, COUNT(*)
                FROM ratings
                WHERE media_id = @media_id";

            using var cmd = new NpgsqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("media_id", mediaId);

            using var reader = cmd.ExecuteReader();
            if (!reader.Read())
                return (null, 0);

            double? avg = reader.IsDBNull(0) ? (double?)null : reader.GetDouble(0);
            var count = reader.GetInt32(1);

            return (avg, count);
        }

        // 10) LEADERBOARD: User sortiert nach Anzahl Ratings
        public List<(int userId, string username, int ratingsCount)> GetLeaderboard(int limit)
        {
            if (limit <= 0) limit = 10;
            if (limit > 100) limit = 100; // Schutz: nicht „unendlich“ liefern

            using var conn = _db.OpenConnection();

            const string sql = @"
                SELECT u.id, u.username, COUNT(r.id) AS ratings_count
                FROM users u
                LEFT JOIN ratings r ON r.user_id = u.id
                GROUP BY u.id, u.username
                ORDER BY ratings_count DESC, u.username ASC
                LIMIT @limit";

            using var cmd = new NpgsqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("limit", limit);

            using var reader = cmd.ExecuteReader();
            var result = new List<(int, string, int)>();

            while (reader.Read())
            {
                var userId = reader.GetInt32(0);
                var username = reader.GetString(1);
                var count = reader.GetInt32(2);
                result.Add((userId, username, count));
            }

            return result;
        }

        private static Rating ReadRating(NpgsqlDataReader reader)
        {
            return new Rating
            {
                Id = reader.GetInt32(0),
                MediaId = reader.GetInt32(1),
                UserId = reader.GetInt32(2),
                Stars = reader.GetInt32(3),
                Comment = reader.IsDBNull(4) ? null : reader.GetString(4),
                CommentConfirmed = reader.GetBoolean(5),
                CreatedAt = reader.GetDateTime(6),
                UpdatedAt = reader.GetDateTime(7),
            };
        }

        public List<(Rating rating, string mediaTitle)> GetHistoryByUser(int userId)
        {
            using var conn = _db.OpenConnection();

            const string sql = @"
                SELECT r.id, r.media_id, r.user_id, r.stars, r.comment, r.comment_confirmed, r.created_at, r.updated_at,
                m.title
                FROM ratings r
                JOIN media m ON m.id = r.media_id
                WHERE r.user_id = @uid
                ORDER BY r.created_at DESC";

            using var cmd = new NpgsqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("uid", userId);

            using var reader = cmd.ExecuteReader();
            var result = new List<(Rating, string)>();

            while (reader.Read())
            {
                var rating = ReadRating(reader);    
                var title = reader.GetString(8);
                result.Add((rating, title));
            }

            return result;
        }

    }
}
