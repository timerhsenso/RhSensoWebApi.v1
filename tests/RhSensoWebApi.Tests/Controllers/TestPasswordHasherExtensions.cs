// Adaptador para permitir .Hash(...) e .Verify(...) nos testes
using Microsoft.AspNetCore.Identity;

public static class TestPasswordHasherExtensions
{
    public static string Hash(this PasswordHasher<object> hasher, string password)
        => hasher.HashPassword(null!, password);

    public static bool Verify(this PasswordHasher<object> hasher, string password, string hash)
        => hasher.VerifyHashedPassword(null!, hash, password) is PasswordVerificationResult.Success;
}
