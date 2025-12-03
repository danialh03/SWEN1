using DhProjekt.Database;
using Npgsql;
using System;

namespace DhProjekt.Database
{
    public class UserRepository
    {
        private readonly DatabaseConnection _db;

        public UserRepository(DatabaseConnection db)
        {
            _db = db;
        }

        public User? GetByUsername(string username)
        {
            using var conn = _db.OpenConnection();

            const string sql = @"SELECT id, username, password_hash
                                 FROM users
                                 WHERE username = @username";

            using var cmd = new NpgsqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("username", username);

            using var reader = cmd.ExecuteReader();
            if (!reader.Read())
                return null;

            return new User
            {
                Id = reader.GetInt32(0),
                Username = reader.GetString(1),
                PasswordHash = reader.GetString(2)
            };
        }

        public User? CreateUser(string username, string passwordHash)
        {
            using var conn = _db.OpenConnection();

            const string sql = @"INSERT INTO users (username, password_hash)
                                 VALUES (@username, @password_hash)
                                 RETURNING id";

            using var cmd = new NpgsqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("username", username);
            cmd.Parameters.AddWithValue("password_hash", passwordHash);

            try
            {
                var idObj = cmd.ExecuteScalar();
                var id = Convert.ToInt32(idObj);

                return new User
                {
                    Id = id,
                    Username = username,
                    PasswordHash = passwordHash
                };
            }
            catch (PostgresException ex) when (ex.SqlState == "23505") // unique violation
            {
                // Username existiert bereits
                return null;
            }
        }
    }
}
