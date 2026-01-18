using System;
using System.Collections.Generic;
using Npgsql;

namespace DhProjekt.Database
{
    /// Zugriff auf die Tabelle "favorites".
    /// Ein Favorite ist eine Verbindung: (user_id, media_id).
    public class FavoriteRepository
    {
        private readonly DatabaseConnection _db;

        public FavoriteRepository(DatabaseConnection db)
        {
            _db = db;
        }

        /// Fügt ein Medium zu den Favoriten des Users hinzu.
        /// Gibt false zurück, wenn es schon existiert (Unique/Primary Key verhindert Duplikate).
        public bool Add(int userId, int mediaId)
        {
            using var conn = _db.OpenConnection();

            const string sql = @"
                INSERT INTO favorites (user_id, media_id)
                VALUES (@user_id, @media_id);";

            using var cmd = new NpgsqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("user_id", userId);
            cmd.Parameters.AddWithValue("media_id", mediaId);

            try
            {
                return cmd.ExecuteNonQuery() > 0;
            }
            catch (PostgresException ex) when (ex.SqlState == "23505")
            {
                // 23505 = unique_violation -> war schon favorisiert
                return false;
            }
        }

        /// Entfernt ein Medium aus den Favoriten des Users.
        /// Gibt false zurück, wenn es nicht existiert.
        public bool Remove(int userId, int mediaId)
        {
            using var conn = _db.OpenConnection();

            const string sql = @"
                DELETE FROM favorites
                WHERE user_id = @user_id AND media_id = @media_id;";

            using var cmd = new NpgsqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("user_id", userId);
            cmd.Parameters.AddWithValue("media_id", mediaId);

            return cmd.ExecuteNonQuery() > 0;
        }

        /// Liefert alle MediaItems, die ein User favorisiert hat.
        /// ich joine auf "media", damit ich die vollständigen Media-Daten zurückgeben kann.
        public List<MediaItem> GetFavoritesByUserId(int userId)
        {
            using var conn = _db.OpenConnection();

            const string sql = @"
                SELECT m.id, m.title, m.description, m.media_type, m.release_year, m.genre, m.age_restriction, m.created_by
                FROM favorites f
                JOIN media m ON m.id = f.media_id
                WHERE f.user_id = @user_id
                ORDER BY m.id;";

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
