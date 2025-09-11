using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using RhSensoWebApi.Core.Common.Exceptions;
using RhSensoWebApi.Core.Interfaces;

namespace RhSensoWebApi.API.Filters;

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = false)]
public sealed class RateLimitAttribute : ActionFilterAttribute
{
    private readonly int _requests;
    private readonly TimeSpan _window;
    private readonly string _keyPrefix;

    public RateLimitAttribute(int requests, int windowSeconds, string keyPrefix = "ratelimit")
    {
        _requests = requests;
        _window = TimeSpan.FromSeconds(windowSeconds);
        _keyPrefix = keyPrefix;
    }

    public override async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var cache = context.HttpContext.RequestServices.GetService<ICacheService>();
        if (cache == null)
        {
            await next();
            return;
        }

        var ip = context.HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        var route = context.HttpContext.Request.Path.ToString().ToLowerInvariant();
        var key = $"{_keyPrefix}:{ip}:{route}";

        var current = await cache.GetAsync<int>(key);

        if (current >= _requests)
        {
            var resp = new BaseResponse<object>
            {
                Success = false,
                Message = "Too many requests",
                Error = new ErrorDto { Code = "TooManyRequests", Message = "Muitas tentativas. Tente novamente mais tarde." },
                TraceId = context.HttpContext.TraceIdentifier
            };
            context.Result = new ObjectResult(resp) { StatusCode = StatusCodes.Status429TooManyRequests };
            return;
        }

        await cache.SetAsync(key, current + 1, _window);
        await next();
    }
}