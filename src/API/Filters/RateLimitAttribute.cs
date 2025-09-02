using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using RhSensoWebApi.Core.Interfaces;              // ICacheService
using RhSensoWebApi.Core.Common.Exceptions;       // BaseResponse<T>, ErrorDto

namespace RhSensoWebApi.API.Filters
{
    /// <summary>
    /// Atributo simples de rate limit por IP+rota usando ICacheService.
    /// Responde 429 com BaseResponse<object> (Success=false, Error).
    /// </summary>
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
            var cache = context.HttpContext.RequestServices.GetService(typeof(ICacheService)) as ICacheService;
            if (cache is null)
            {
                await next();
                return;
            }

            var ip = context.HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            var route = context.HttpContext.Request.Path.ToString().ToLowerInvariant();
            var key = $"{_keyPrefix}:{ip}:{route}";

            var current = await cache.GetAsync<int?>(key) ?? 0;
            if (current >= _requests)
            {
                var resp = new BaseResponse<object>
                {
                    Success = false,
                    Error = new ErrorDto
                    {
                        Code = "TooManyRequests",
                        Message = "Muitas tentativas. Tente novamente mais tarde."
                    }
                };
                context.Result = new ObjectResult(resp) { StatusCode = StatusCodes.Status429TooManyRequests };
                return;
            }

            await cache.SetAsync(key, current + 1, _window);
            await next();
        }
    }
}
