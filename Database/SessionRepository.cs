using System;
using Npgsql;

namespace DhProjekt.Database
{
    public class SessionRepository
    {
        private readonly DatabaseConnection _db;

        public SessionRepository(DatabaseConnection db)
        {
            _db = db;
        }

        public void CreateSession(string token, int userId, DateTime expiresAtUtc)
        {
            using var conn = _db.OpenConnection();

            const string sql = @"
                INSERT INTO sessions (token, user_id, expires_at)
                VALUES (@token, @user_id, @expires_at)";

            using var cmd = new NpgsqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("token", token);
            cmd.Parameters.AddWithValue("user_id", userId);
            cmd.Parameters.AddWithValue("expires_at", expiresAtUtc);

            cmd.ExecuteNonQuery();
        }

        public int? GetUserIdForToken(string token)
        {
            using var conn = _db.OpenConnection();

            const string sql = @"
                SELECT user_id, expires_at
                FROM sessions
                WHERE token = @token";

            using var cmd = new NpgsqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("token", token);

            using var reader = cmd.ExecuteReader();
            if (!reader.Read())
                return null;

            var userId = reader.GetInt32(0);
            var expiresAt = reader.GetDateTime(1);

            // abgelaufen? -> löschen und ungültig
            if (DateTime.UtcNow > expiresAt.ToUniversalTime())
            {
                reader.Close();
                DeleteToken(token);
                return null;
            }

            return userId;
        }

        public bool DeleteToken(string token)
        {
            using var conn = _db.OpenConnection();

            const string sql = @"DELETE FROM sessions WHERE token = @token";

            using var cmd = new NpgsqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("token", token);

            return cmd.ExecuteNonQuery() > 0;
        }
    }
}
