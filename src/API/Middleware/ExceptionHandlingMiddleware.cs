using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
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
            await WriteExceptionAsync(context, ex);
        }
    }

    private static async Task WriteExceptionAsync(HttpContext ctx, Exception ex)
    {
        var (status, code, message, errors) = MapException(ex);

        ctx.Response.StatusCode = status;
        ctx.Response.ContentType = "application/json";

        var response = new BaseResponse<object?>
        {
            Success = false,
            Message = message,
            Data = null,
            Error = new ErrorDto { Code = code, Message = message },
            // Para manter compatibilidade com os testes:
            // - Se NÃO mapeado (500), garantimos errors["exception"].
            // - Se mapeado e houver um dicionário de errors, usamos ele.
            Errors = errors ?? (status == StatusCodes.Status500InternalServerError
                ? new Dictionary<string, string[]> { ["exception"] = new[] { ex.Message } }
                : null),
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

    private static (int status, string code, string message, IDictionary<string, string[]>? errors) MapException(Exception ex)
    {
        // Mapeia casos conhecidos para status/código/mensagem.
        // Ajuste livremente conforme suas exceções de domínio.
        return ex switch
        {
            KeyNotFoundException => (StatusCodes.Status404NotFound, "NOT_FOUND", ex.Message, null),
            UnauthorizedAccessException => (StatusCodes.Status403Forbidden, "FORBIDDEN", "Acesso negado.", null),
            ArgumentException => (StatusCodes.Status400BadRequest, "BAD_REQUEST", ex.Message, null),
            DbUpdateException => (StatusCodes.Status409Conflict, "CONFLICT", "Conflito ao persistir dados.", null),

            // Tentativa genérica de capturar exceções de validação sem acoplar a FluentValidation.
            // Se você usa FluentValidation, podemos evoluir para extrair o dicionário de errors.
            var e when e.GetType().Name == "ValidationException"
                => (StatusCodes.Status400BadRequest, "VALIDATION_ERROR", "Erro de validação.",
                    new Dictionary<string, string[]> { ["validation"] = new[] { ex.Message } }),

            _ => (StatusCodes.Status500InternalServerError, "INTERNAL_ERROR", "Erro interno do servidor.", null)
        };
    }
}
