using System;
using System.Security.Cryptography;

namespace DhProjekt.Auth
{
    public static class PasswordHelper
    {
        // PBKDF2$<iterations>$<saltBase64>$<hashBase64>
        // Example: PBKDF2$100000$...$...
        private const string Prefix = "PBKDF2$";

        private const int SaltSizeBytes = 16;          // 128-bit salt
        private const int KeySizeBytes = 32;           // 256-bit derived key
        private const int Iterations = 100_000;        // ggf. 50k-200k ok
        private static readonly HashAlgorithmName Algo = HashAlgorithmName.SHA256;


        /// Erzeugt einen salted PBKDF2 Hash im Format: PBKDF2$iters$salt$hash
        public static string HashPassword(string password)
        {
            if (password == null) throw new ArgumentNullException(nameof(password));

            byte[] salt = RandomNumberGenerator.GetBytes(SaltSizeBytes);

            byte[] dk = Rfc2898DeriveBytes.Pbkdf2(
                password: password,
                salt: salt,
                iterations: Iterations,
                hashAlgorithm: Algo,
                outputLength: KeySizeBytes);

            return $"{Prefix}{Iterations}${Convert.ToBase64String(salt)}${Convert.ToBase64String(dk)}";
        }

        /// Verifiziert Passwort gegen gespeicherten Hash.
        public static bool VerifyPassword(string password, string storedHash)
        {
            if (string.IsNullOrWhiteSpace(password) || string.IsNullOrWhiteSpace(storedHash))
                return false;

            if (storedHash.StartsWith(Prefix, StringComparison.OrdinalIgnoreCase))
            {
                return VerifyPbkdf2(password, storedHash);
            }

            // Altes Format: SHA256 Hex (so wie mein bisheriges)
            // -> so bleiben bestehende DB-User loginfähig
            return string.Equals(LegacySha256Hex(password), storedHash, StringComparison.OrdinalIgnoreCase);
        }

        private static bool VerifyPbkdf2(string password, string stored)
        {
            // PBKDF2$iters$salt$hash
            var parts = stored.Split('$', StringSplitOptions.RemoveEmptyEntries);
            // parts: ["PBKDF2", "100000", "<saltB64>", "<hashB64>"]
            if (parts.Length != 4)
                return false;

            if (!int.TryParse(parts[1], out int iters) || iters <= 0)
                return false;

            byte[] salt;
            byte[] expected;
            try
            {
                salt = Convert.FromBase64String(parts[2]);
                expected = Convert.FromBase64String(parts[3]);
            }
            catch
            {
                return false;
            }

            byte[] actual = Rfc2898DeriveBytes.Pbkdf2(
                password: password,
                salt: salt,
                iterations: iters,
                hashAlgorithm: Algo,
                outputLength: expected.Length);

            return CryptographicOperations.FixedTimeEquals(actual, expected);
        }

        // --- Legacy SHA256 (damit alte Hashes noch funktionieren) ---
        private static string LegacySha256Hex(string password)
        {
            using var sha = SHA256.Create();
            var bytes = System.Text.Encoding.UTF8.GetBytes(password);
            var hash = sha.ComputeHash(bytes);
            return Convert.ToHexString(hash); // gleich wie vorher
        }
    }
}
