using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using RhSensoWebApi.API.Filters;
using RhSensoWebApi.Core.Interfaces;

namespace RhSensoWebApi.Tests.Filters;

public class RateLimitAttributeTests
{
    [Fact]
    public async Task Exceeding_Limit_Should_Set_429()
    {
        // Mock do cache: contador em int? (nullable), exatamente como o filtro usa
        var cache = new Mock<ICacheService>();
        int? counter = 0;

        cache.Setup(x => x.GetAsync<int?>(It.IsAny<string>()))
             .ReturnsAsync(() => counter);

        cache.Setup(x => x.SetAsync<int?>(It.IsAny<string>(), It.IsAny<int?>(), It.IsAny<TimeSpan?>()))
             .Callback<string, int?, TimeSpan?>((k, v, exp) => counter = v)
             .Returns(Task.CompletedTask);

        // DI para o filtro resolver ICacheService
        var services = new ServiceCollection();
        services.AddSingleton(cache.Object);
        var sp = services.BuildServiceProvider();

        // HttpContext estável (mesmo Path/IP) para gerar a mesma chave de cache
        var http = new DefaultHttpContext { RequestServices = sp };
        http.Request.Path = "/api/test";
        http.Connection.RemoteIpAddress = IPAddress.Parse("127.0.0.1");

        var actionContext = new ActionContext(
            http,
            new RouteData(),
            new ActionDescriptor(),
            new ModelStateDictionary());

        var filters = new List<IFilterMetadata>();
        var actionArgs = new Dictionary<string, object?>();
        var ctx = new ActionExecutingContext(actionContext, filters, actionArgs, controller: new object());

        bool nextCalled = false;
        var next = new ActionExecutionDelegate(async () =>
        {
            nextCalled = true;
            return await Task.FromResult(new ActionExecutedContext(actionContext, filters, controller: new object()));
        });

        // Limite: 2 req / 60s → 3ª deve bloquear
        var attr = new RateLimitAttribute(2, 60);

        await attr.OnActionExecutionAsync(ctx, next);
        nextCalled.Should().BeTrue();
        ctx.Result.Should().BeNull();

        nextCalled = false;
        await attr.OnActionExecutionAsync(ctx, next);
        nextCalled.Should().BeTrue();
        ctx.Result.Should().BeNull();

        nextCalled = false;
        await attr.OnActionExecutionAsync(ctx, next);
        nextCalled.Should().BeFalse(); // 3ª chamada deve ser bloqueada
        ctx.Result.Should().BeOfType<ObjectResult>();
        ((ObjectResult)ctx.Result!).StatusCode.Should().Be(429);
    }
}
