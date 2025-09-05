using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using RhSensoWebApi.Core.Entities;
// AJUSTE: troque os usings abaixo conforme seus namespaces reais
using RhSensoWebApi.Infrastructure.Services;

namespace RhSensoWebApi.Tests.Services;

public class TokenServiceTests
{
    [Fact]
    public void Generate_And_Validate_Token()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["JWT:Key"] = "dev-key-256-bits-1234567890ABCdefghiJKLmnOPqrSTUv",
                ["JWT:Issuer"] = "RhSensoWebApi",
                ["JWT:Audience"] = "RhSensoWebApi-Clients",
                ["JWT:ExpiryMinutes"] = "15"
            }!)
            .Build();

        var svc = new TokenService(config, new NullLogger<TokenService>());
        var user = new User { Id = 1, CdUsuario = "admin", FlAtivo = true };

        var token = svc.GenerateToken(user, permissions: []);
        token.Should().NotBeNullOrWhiteSpace();
        svc.ValidateToken(token).Should().BeTrue();

        var principal = svc.GetPrincipalFromToken(token);
        principal.Identity!.IsAuthenticated.Should().BeTrue();
    }
}
