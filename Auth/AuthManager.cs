using System;
using System.Collections.Generic;

namespace DhProjekt.Auth
{
    public static class AuthManager
    {
        // token -> userId
        private static readonly Dictionary<string, int> _tokens = new();
        private static readonly object _lock = new();

        public static string CreateToken(int userId, string username)
        {
            var token = $"{username}-mrpToken-{Guid.NewGuid()}";

            lock (_lock)
            {
                _tokens[token] = userId;
            }

            return token;
        }

        public static int? GetUserIdForToken(string token)
        {
            lock (_lock)
            {
                return _tokens.TryGetValue(token, out var userId)
                    ? userId
                    : (int?)null;
            }
        }
    }
}
