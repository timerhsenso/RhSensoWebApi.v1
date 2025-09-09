using System.Diagnostics;
using Serilog.Context;                 // enriquece logs com a propriedade CorrelationId
using Microsoft.AspNetCore.Http;

namespace RhSensoWebApi.API.Middleware
{
    /// <summary>
    /// Garante um X-Correlation-Id para cada requisição:
    /// - aceita cabeçalhos de entrada (X-Correlation-Id / X-Request-ID) ou gera um novo;
    /// - coloca no HttpContext.TraceIdentifier (alinha com o que você já retorna no BaseResponse/ProblemDetails);
    /// - adiciona o cabeçalho X-Correlation-Id na resposta;
    /// - enriquece Serilog com a propriedade "CorrelationId".
    /// </summary>
    public sealed class CorrelationIdMiddleware
    {
        public const string HeaderName = "X-Correlation-Id";
        private readonly RequestDelegate _next;

        public CorrelationIdMiddleware(RequestDelegate next) => _next = next;

        public async Task Invoke(HttpContext context)
        {
            // 1) Tenta obter do header, senão usa Activity.TraceId, senão GUID.
            var incoming =
                context.Request.Headers[HeaderName].ToString()
                ?? context.Request.Headers["X-Request-ID"].ToString();

            var correlationId =
                !string.IsNullOrWhiteSpace(incoming) ? incoming :
                (Activity.Current?.TraceId.ToString() ?? Guid.NewGuid().ToString("n"));

            // 2) Alinha com pipeline ASP.NET
            context.TraceIdentifier = correlationId;

            // 3) Retorna para o cliente
            context.Response.OnStarting(() =>
            {
                if (!context.Response.Headers.ContainsKey(HeaderName))
                    context.Response.Headers[HeaderName] = correlationId;
                return Task.CompletedTask;
            });

            // 4) Enriquecimento do Serilog
            using (LogContext.PushProperty("CorrelationId", correlationId))
            {
                await _next(context);
            }
        }
    }
}
