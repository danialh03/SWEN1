using System;

namespace DhProjekt.Helpers
{
    public static class MediaSortHelper
    {
        // Liefert eine sichere ORDER BY Expression (nur erlaubte Spalten)
        public static string GetOrderByExpression(string? sort)
        {
            var key = (sort ?? "").Trim().ToLowerInvariant();

            return key switch
            {
                "title" => "m.title",
                "year" => "m.release_year",
                "score" => "COALESCE(r.avg_score, 0)",
                _ => "m.id"
            };
        }

        public static string GetOrderDirection(string? order)
        {
            var dir = (order ?? "").Trim().ToLowerInvariant();
            return dir == "desc" ? "DESC" : "ASC";
        }
    }
}
