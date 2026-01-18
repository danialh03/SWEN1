using System;
using System.Collections.Generic;

namespace DhProjekt.Helpers
{
    public static class QueryStringHelper
    {
        // "?a=1&b=hello%20world" -> Dictionary
        public static Dictionary<string, string> Parse(string? query)
        {
            var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            if (string.IsNullOrWhiteSpace(query) || query == "?")
                return result;

            var q = query.StartsWith("?") ? query.Substring(1) : query;
            if (string.IsNullOrWhiteSpace(q))
                return result;

            var parts = q.Split('&', StringSplitOptions.RemoveEmptyEntries);
            foreach (var part in parts)
            {
                var kv = part.Split('=', 2);
                var key = Uri.UnescapeDataString(kv[0]);
                var value = kv.Length > 1 ? Uri.UnescapeDataString(kv[1]) : "";

                if (!string.IsNullOrWhiteSpace(key))
                    result[key] = value;
            }

            return result;
        }
    }
}
