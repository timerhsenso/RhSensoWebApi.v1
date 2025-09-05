using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using RhSensoWebApi.Core.Common.Exceptions;

namespace RhSensoWebApi.API.Middleware;

public sealed class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    // Os testes chamam Invoke(context), então mantenha este nome/assinatura
    public async Task Invoke(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception");
            await WriteUnhandledAsync(context, ex);
        }
    }

    private static async Task WriteUnhandledAsync(HttpContext ctx, Exception ex)
    {
        // 500 + JSON padronizado
        ctx.Response.StatusCode = StatusCodes.Status500InternalServerError;
        ctx.Response.ContentType = "application/json";

        // Preenche tanto 'error' (erro geral) quanto 'errors' (detalhe p/ testes)
        var message = "Erro interno do servidor.";
        var response = new BaseResponse<object?>
        {
            Success = false,
            Message = message,
            Data = null,
            Error = new ErrorDto { Code = "INTERNAL_ERROR", Message = message },
            // Importante: fornecer 'errors' não-nulo para o caso de exceção não tratada.
            // A chave pode ser qualquer uma estável (ex.: "exception"); o teste apenas verifica a existência.
            Errors = new Dictionary<string, string[]>
            {
                ["exception"] = new[] { ex.Message }
            },
            TraceId = ctx.TraceIdentifier,
            Timestamp = DateTime.UtcNow
        };

        var json = JsonSerializer.Serialize(
            response,
            new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
            });

        await ctx.Response.WriteAsync(json);
    }
}
