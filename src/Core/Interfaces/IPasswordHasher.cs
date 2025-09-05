namespace RhSensoWebApi.Core.Interfaces
{
    /// <summary>
    /// Hasher de senhas para autenticação em tabelas legadas.
    /// </summary>
    public interface IPasswordHasher
    {
        /// <summary>Gera o hash seguro da senha.</summary>
        string HashPassword(string password);

        /// <summary>Valida a senha em texto puro contra o hash armazenado.</summary>
        bool VerifyPassword(string password, string passwordHash);
    }
}
