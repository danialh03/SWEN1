using System;

namespace DhProjekt.Helpers
{
    public static class AuthHeaderHelper
    {
        // Nimmt "Bearer abc123" und gibt "abc123" zurück, sonst null
        public static string? ExtractBearerToken(string? headerValue)
        {
            if (string.IsNullOrWhiteSpace(headerValue))
                return null;

            const string prefix = "Bearer ";
            if (!headerValue.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                return null;

            var token = headerValue.Substring(prefix.Length).Trim();
            return string.IsNullOrWhiteSpace(token) ? null : token;
        }

        // Standard: Authorization, Spec: Authentication (du unterstützt beide)
        public static string? TryGetBearerToken(string? authorizationHeader, string? authenticationHeader)
        {
            return ExtractBearerToken(authenticationHeader) ?? ExtractBearerToken(authorizationHeader);
        }
    }
}
