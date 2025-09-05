using FluentAssertions;
using RhSensoWebApi.Infrastructure.Services;

namespace RhSensoWebApi.Tests.Services;

public class PasswordHasherTests
{
    [Fact]
    public void Hash_And_Verify_Works()
    {
        var hasher = new PasswordHasher();
        var hash = hasher.HashPassword("123456");
        hash.Should().NotBeNullOrWhiteSpace();
        hasher.VerifyPassword("123456", hash).Should().BeTrue();
        hasher.VerifyPassword("senha_errada", hash).Should().BeFalse();
    }
}
