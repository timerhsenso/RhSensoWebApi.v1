using Microsoft.Extensions.Logging;
using RhSensoWebApi.Core.Interfaces;
using System.Security.Cryptography;
using System.Text;

namespace RhSensoWebApi.Infrastructure.Services
{
    public class PasswordHasher : IPasswordHasher
    {
        private readonly ILogger<PasswordHasher> _logger;

        public PasswordHasher(ILogger<PasswordHasher> logger) { _logger = logger; }

        public string HashPassword(string password) => BCrypt.Net.BCrypt.HashPassword(password);

        public bool VerifyPassword(string password, string hashedPassword)
        {
            try
            {
                if (string.IsNullOrEmpty(password) || string.IsNullOrEmpty(hashedPassword)) return false;

                if (hashedPassword.StartsWith("$2a$") || hashedPassword.StartsWith("$2b$") || hashedPassword.StartsWith("$2y$"))
                    return BCrypt.Net.BCrypt.Verify(password, hashedPassword);

                if (IsBase64Sha256(hashedPassword))
                {
                    using var sha256 = SHA256.Create();
                    var hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
                    var b64 = Convert.ToBase64String(hash);
                    return string.Equals(b64, hashedPassword, StringComparison.Ordinal);
                }

                return string.Equals(password, hashedPassword, StringComparison.Ordinal);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao verificar senha");
                return false;
            }
        }

        private static bool IsBase64Sha256(string s)
        {
            if (s.Length != 44) return false;
            try { return Convert.FromBase64String(s).Length == 32; } catch { return false; }
        }
    }
}
