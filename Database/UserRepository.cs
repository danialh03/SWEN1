using System;
using Npgsql;

namespace DhProjekt.Database
{
    public class UserRepository
    {
        private readonly DatabaseConnection _db;

        public UserRepository(DatabaseConnection db)
        {
            _db = db;
        }

        // 1) REGISTER / LOGIN
        public User? GetByUsername(string username)
        {
            using var conn = _db.OpenConnection();

            const string sql = @"
                SELECT id, username, password_hash, display_name, bio, updated_at
                FROM users
                WHERE username = @username";

            using var cmd = new NpgsqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("username", username);

            using var reader = cmd.ExecuteReader();
            if (!reader.Read())
                return null;

            return ReadUser(reader);
        }

        public User? GetById(int id)
        {
            using var conn = _db.OpenConnection();

            const string sql = @"
                SELECT id, username, password_hash, display_name, bio, updated_at
                FROM users
                WHERE id = @id";

            using var cmd = new NpgsqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("id", id);

            using var reader = cmd.ExecuteReader();
            if (!reader.Read())
                return null;

            return ReadUser(reader);
        }

        public User? CreateUser(string username, string passwordHash)
        {
            using var conn = _db.OpenConnection();

            const string sql = @"
                INSERT INTO users (username, password_hash)
                VALUES (@username, @password_hash)
                RETURNING id, username, password_hash, display_name, bio, updated_at";

            using var cmd = new NpgsqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("username", username);
            cmd.Parameters.AddWithValue("password_hash", passwordHash);

            try
            {
                using var reader = cmd.ExecuteReader();
                if (!reader.Read())
                    return null;

                return ReadUser(reader);
            }
            catch (PostgresException ex) when (ex.SqlState == "23505")
            {
                // Username existiert bereits
                return null;
            }
        }

        // 2) PROFILE
        // Extra Methode nur für bessere Lesbarkeit 
        public User? GetProfileByUsername(string username)
            => GetByUsername(username);

        public bool UpdateProfile(int userId, string? displayName, string? bio)
        {
            using var conn = _db.OpenConnection();

            const string sql = @"
                UPDATE users
                SET display_name = @display_name,
                    bio = @bio,
                    updated_at = NOW()
                WHERE id = @id";

            using var cmd = new NpgsqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("id", userId);
            cmd.Parameters.AddWithValue("display_name", (object?)displayName ?? DBNull.Value);
            cmd.Parameters.AddWithValue("bio", (object?)bio ?? DBNull.Value);

            return cmd.ExecuteNonQuery() > 0;
        }

        public (int totalRatings, double? avgStars, string? favoriteGenre, int favoritesCount) GetUserStats(int userId)
        {
            using var conn = _db.OpenConnection();

            // total + avg
            int totalRatings;
            double? avgStars;
            {
                const string sql = @"SELECT COUNT(*), AVG(stars)::float8 FROM ratings WHERE user_id=@uid";
                using var cmd = new NpgsqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("uid", userId);

                using var r = cmd.ExecuteReader();
                r.Read();
                totalRatings = r.GetInt32(0);
                avgStars = r.IsDBNull(1) ? (double?)null : r.GetDouble(1);
            }

            // favoriteGenre
            string? favoriteGenre;
            {
                const string sql = @"
                    SELECT m.genre
                    FROM ratings r
                    JOIN media m ON m.id = r.media_id
                    WHERE r.user_id = @uid AND m.genre IS NOT NULL
                    GROUP BY m.genre
                    ORDER BY COUNT(*) DESC
                    LIMIT 1";

                using var cmd = new NpgsqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("uid", userId);

                var obj = cmd.ExecuteScalar();
                favoriteGenre = obj == null ? null : obj.ToString();
            }

            // favoritesCount
            int favoritesCount;
            {
                const string sql = @"SELECT COUNT(*) FROM favorites WHERE user_id=@uid";
                using var cmd = new NpgsqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("uid", userId);

                favoritesCount = Convert.ToInt32(cmd.ExecuteScalar());
            }

            return (totalRatings, avgStars, favoriteGenre, favoritesCount);
        }

        // Helper
        private static User ReadUser(NpgsqlDataReader reader)
        {
            return new User
            {
                Id = reader.GetInt32(0),
                Username = reader.GetString(1),
                PasswordHash = reader.GetString(2),
                DisplayName = reader.IsDBNull(3) ? null : reader.GetString(3),
                Bio = reader.IsDBNull(4) ? null : reader.GetString(4),
                UpdatedAt = reader.GetDateTime(5)
            };
        }
    }
}
