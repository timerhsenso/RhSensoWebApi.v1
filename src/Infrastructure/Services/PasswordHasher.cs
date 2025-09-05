using RhSensoWebApi.Core.Interfaces;

namespace RhSensoWebApi.Infrastructure.Services
{
    /// <summary>
    /// Implementação de IPasswordHasher com BCrypt (sem depender de ASP.NET Identity).
    /// </summary>
    public sealed class PasswordHasher : IPasswordHasher
    {
        private const int WorkFactor = 12; // ajuste conforme necessário

        public string HashPassword(string password)
        {
            if (string.IsNullOrWhiteSpace(password))
                throw new ArgumentException("Senha não pode ser vazia.", nameof(password));

            return BCrypt.Net.BCrypt.HashPassword(password, workFactor: WorkFactor);
        }

        public bool VerifyPassword(string password, string passwordHash)
        {
            if (string.IsNullOrWhiteSpace(passwordHash))
                return false;

            return BCrypt.Net.BCrypt.Verify(password, passwordHash);
        }
    }
}
