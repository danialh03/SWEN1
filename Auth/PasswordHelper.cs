using System.Security.Cryptography;
using System.Text;

namespace DhProjekt.Auth
{
    public static class PasswordHelper
    {
        // SHA256-Hash-Funktion 
        public static string HashPassword(string password)
        {
            using var sha = SHA256.Create();
            var bytes = Encoding.UTF8.GetBytes(password);
            var hash = sha.ComputeHash(bytes);
            return Convert.ToHexString(hash);
        }
    }
}
