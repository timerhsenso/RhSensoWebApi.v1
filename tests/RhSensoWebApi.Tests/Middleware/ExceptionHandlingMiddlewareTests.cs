using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
// AJUSTE: middleware real
using RhSensoWebApi.API.Middleware;

namespace RhSensoWebApi.Tests.Middleware;

public class ExceptionHandlingMiddlewareTests
{
    [Fact]
    public async Task Unhandled_Exception_Returns_500_BaseResponse()
    {
        var context = new DefaultHttpContext();
        var responseBody = new MemoryStream();
        context.Response.Body = responseBody;

        RequestDelegate next = _ => throw new InvalidOperationException("boom");

        var mw = new ExceptionHandlingMiddleware(next, new NullLogger<ExceptionHandlingMiddleware>());
        await mw.Invoke(context);

        context.Response.StatusCode.Should().Be(StatusCodes.Status500InternalServerError);

        responseBody.Position = 0;
        using var doc = await JsonDocument.ParseAsync(responseBody);
        var root = doc.RootElement;

        root.GetProperty("success").GetBoolean().Should().BeFalse();
        root.TryGetProperty("errors", out _).Should().BeTrue();
        root.TryGetProperty("traceId", out _).Should().BeTrue();
    }
}
