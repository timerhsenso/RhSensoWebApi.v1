using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using RhSensoWebApi.Tests.Common;
using RhSensoWebApi.Core.DTOs;

namespace RhSensoWebApi.Tests.Controllers;

public class AuthController_Login_Tests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;
    public AuthController_Login_Tests(CustomWebApplicationFactory factory) => _factory = factory;

    [Fact]
    public async Task Login_ComCredenciais_Validas_Retorna_200_Com_Token()
    {
        var client = _factory.CreateClient();

        var resp = await client.PostAsJsonAsync("/api/v1/auth/login", new
        {
            CdUsuario = "admin",
            Senha = "123"
        });

        resp.StatusCode.Should().Be(HttpStatusCode.OK);

        var obj = await resp.Content.ReadFromJsonAsync<LoginResponse>();
        obj.Should().NotBeNull();
        obj!.Success.Should().BeTrue();
        obj.Data.Should().NotBeNull();
        obj.Data!.Token.Should().NotBeNullOrEmpty();
        obj.Data!.TokenType.Should().Be("Bearer");
        obj.Data!.UserInfo.Should().NotBeNull();
        obj.Data!.UserInfo!.CdUsuario.Should().Be("admin");
    }

    [Fact]
    public async Task Login_ComModel_Invalido_Retorna_400_BaseResponse()
    {
        var client = _factory.CreateClient();

        var resp = await client.PostAsJsonAsync("/api/v1/auth/login", new
        {
            CdUsuario = "",
            Senha = ""
        });

        resp.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var obj = await resp.Content.ReadFromJsonAsync<LoginResponse>();
        obj.Should().NotBeNull();
        obj!.Success.Should().BeFalse();
        obj.Errors.Should().NotBeNull();
    }
}
