using DhProjekt.Database;
using Npgsql;
using System;
using System.Collections.Generic;

namespace DhProjekt.Database
{
    public class MediaRepository
    {
        private readonly DatabaseConnection _db;

        public MediaRepository(DatabaseConnection db)
        {
            _db = db;
        }

        public MediaItem Create(MediaItem item)
        {
            using var conn = _db.OpenConnection();

            const string sql = @"
                INSERT INTO media (title, description, media_type, release_year, genre, age_restriction, created_by)
                VALUES (@title, @description, @media_type, @release_year, @genre, @age_restriction, @created_by)
                RETURNING id";

            using var cmd = new NpgsqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("title", item.Title);
            cmd.Parameters.AddWithValue("description", (object?)item.Description ?? DBNull.Value);
            cmd.Parameters.AddWithValue("media_type", (object?)item.MediaType ?? DBNull.Value);
            cmd.Parameters.AddWithValue("release_year", (object?)item.ReleaseYear ?? DBNull.Value);
            cmd.Parameters.AddWithValue("genre", (object?)item.Genre ?? DBNull.Value);
            cmd.Parameters.AddWithValue("age_restriction", (object?)item.AgeRestriction ?? DBNull.Value);
            cmd.Parameters.AddWithValue("created_by", item.CreatedBy);

            var idObj = cmd.ExecuteScalar();
            item.Id = Convert.ToInt32(idObj);

            return item;
        }

        public List<MediaItem> GetAll()
        {
            using var conn = _db.OpenConnection();

            const string sql = @"
                SELECT id, title, description, media_type, release_year, genre, age_restriction, created_by
                FROM media
                ORDER BY id";

            using var cmd = new NpgsqlCommand(sql, conn);
            using var reader = cmd.ExecuteReader();

            var result = new List<MediaItem>();

            while (reader.Read())
            {
                result.Add(ReadMedia(reader));
            }

            return result;
        }

        public MediaItem? GetById(int id)
        {
            using var conn = _db.OpenConnection();

            const string sql = @"
                SELECT id, title, description, media_type, release_year, genre, age_restriction, created_by
                FROM media
                WHERE id = @id";

            using var cmd = new NpgsqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("id", id);

            using var reader = cmd.ExecuteReader();
            if (!reader.Read())
                return null;

            return ReadMedia(reader);
        }

        public bool Update(MediaItem item, int userId)
        {
            using var conn = _db.OpenConnection();

            const string sql = @"
                UPDATE media
                SET title = @title,
                    description = @description,
                    media_type = @media_type,
                    release_year = @release_year,
                    genre = @genre,
                    age_restriction = @age_restriction
                WHERE id = @id AND created_by = @userId";

            using var cmd = new NpgsqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("title", item.Title);
            cmd.Parameters.AddWithValue("description", (object?)item.Description ?? DBNull.Value);
            cmd.Parameters.AddWithValue("media_type", (object?)item.MediaType ?? DBNull.Value);
            cmd.Parameters.AddWithValue("release_year", (object?)item.ReleaseYear ?? DBNull.Value);
            cmd.Parameters.AddWithValue("genre", (object?)item.Genre ?? DBNull.Value);
            cmd.Parameters.AddWithValue("age_restriction", (object?)item.AgeRestriction ?? DBNull.Value);
            cmd.Parameters.AddWithValue("id", item.Id);
            cmd.Parameters.AddWithValue("userId", userId);

            var rows = cmd.ExecuteNonQuery();
            return rows > 0;
        }

        public bool Delete(int id, int userId)
        {
            using var conn = _db.OpenConnection();

            const string sql = @"DELETE FROM media WHERE id = @id AND created_by = @userId";

            using var cmd = new NpgsqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("id", id);
            cmd.Parameters.AddWithValue("userId", userId);

            var rows = cmd.ExecuteNonQuery();
            return rows > 0;
        }

        private static MediaItem ReadMedia(NpgsqlDataReader reader)
        {
            return new MediaItem
            {
                Id = reader.GetInt32(0),
                Title = reader.GetString(1),
                Description = reader.IsDBNull(2) ? null : reader.GetString(2),
                MediaType = reader.IsDBNull(3) ? null : reader.GetString(3),
                ReleaseYear = reader.IsDBNull(4) ? null : reader.GetInt32(4),
                Genre = reader.IsDBNull(5) ? null : reader.GetString(5),
                AgeRestriction = reader.IsDBNull(6) ? null : reader.GetInt32(6),
                CreatedBy = reader.GetInt32(7)
            };
        }
    }
}
