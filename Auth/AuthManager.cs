using System;
using DhProjekt.Database;

namespace DhProjekt.Auth
{
    public static class AuthManager
    {
        public static readonly TimeSpan TokenLifetime = TimeSpan.FromHours(12);

        public static string GenerateToken()
        {
            return Guid.NewGuid().ToString("N");
        }


        // Token erstellen + direkt in DB speichern (pro User zugeordnet)
        public static string CreateToken(int userId, SessionRepository sessions)
        {
            var token = GenerateToken();

            var expiresAtUtc = DateTime.UtcNow.Add(TokenLifetime);

            sessions.CreateSession(token, userId, expiresAtUtc);
            return token;
        }

        // Token validieren über DB (inkl. Ablauf)
        public static int? GetUserIdForToken(string token, SessionRepository sessions)
        {
            return sessions.GetUserIdForToken(token);
        }
    }
}
