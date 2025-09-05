using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using RhSensoWebApi.API.Common; // OkResponse / FailResponse

namespace RhSensoWebApi.Tests.Api;

public class ControllerResponseExtensionsTests
{
    private sealed class DummyController : ControllerBase
    {
        // Helper: garante HttpContext antes de usar as extensões
        public void EnsureHttpContext()
        {
            if (ControllerContext?.HttpContext == null)
            {
                ControllerContext = new ControllerContext
                {
                    HttpContext = new DefaultHttpContext()
                };
            }
        }

        public IActionResult OkResp(object data)
        {
            EnsureHttpContext();
            return this.OkResponse(data);
        }

        // Atenção à ordem dos parâmetros da extensão:
        // FailResponse(this ControllerBase c, int statusCode, string message, string? code = null, IDictionary<string,string[]>? errors = null)
        public IActionResult FailResp(int status, string code, string message, IDictionary<string, string[]>? errors = null)
        {
            EnsureHttpContext();
            return this.FailResponse(status, message, code, errors);
        }
    }

    [Fact]
    public void OkResponse_Should_Wrap_Data_As_BaseResponse_200()
    {
        var controller = new DummyController();
        var result = controller.OkResp(new { Name = "Alice" }) as ObjectResult;

        result.Should().NotBeNull();
        result!.StatusCode.Should().Be(StatusCodes.Status200OK);

        var options = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
        var json = JsonSerializer.Serialize(result.Value, options);
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        root.TryGetProperty("success", out var successProp).Should().BeTrue();
        successProp.GetBoolean().Should().BeTrue();

        root.TryGetProperty("data", out var dataProp).Should().BeTrue();
        dataProp.ValueKind.Should().Be(JsonValueKind.Object);
        dataProp.TryGetProperty("name", out var nameProp).Should().BeTrue();
        nameProp.GetString().Should().Be("Alice");

        root.TryGetProperty("traceId", out var traceProp).Should().BeTrue();
        traceProp.ValueKind.Should().Be(JsonValueKind.String);
    }

    [Fact]
    public void FailResponse_Should_Return_Custom_Status_And_Error()
    {
        var controller = new DummyController();

        var errors = new Dictionary<string, string[]>
        {
            ["login"] = new[] { "Credenciais inválidas." }
        };

        // 403 com código e mensagem custom
        var result = controller.FailResp(StatusCodes.Status403Forbidden, "FORBIDDEN", "Acesso negado", errors) as ObjectResult;

        result.Should().NotBeNull();
        result!.StatusCode.Should().Be(StatusCodes.Status403Forbidden);

        var options = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
        var json = JsonSerializer.Serialize(result.Value, options);
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        root.TryGetProperty("success", out var successProp).Should().BeTrue();
        successProp.GetBoolean().Should().BeFalse();

        root.TryGetProperty("error", out var errorProp).Should().BeTrue();
        errorProp.TryGetProperty("code", out var codeProp).Should().BeTrue();
        codeProp.GetString().Should().Be("FORBIDDEN");
        errorProp.TryGetProperty("message", out var msgProp).Should().BeTrue();
        msgProp.GetString().Should().Be("Acesso negado");

        root.TryGetProperty("errors", out var errorsProp).Should().BeTrue();
        errorsProp.ValueKind.Should().Be(JsonValueKind.Object);
        errorsProp.TryGetProperty("login", out var loginErrors).Should().BeTrue();
        loginErrors.EnumerateArray().First().GetString().Should().Be("Credenciais inválidas.");

        root.TryGetProperty("traceId", out var traceProp).Should().BeTrue();
        traceProp.ValueKind.Should().Be(JsonValueKind.String);
    }
}
