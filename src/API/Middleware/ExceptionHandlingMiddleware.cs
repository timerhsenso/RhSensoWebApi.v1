using System.Net;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using RhSensoWebApi.Core.Common.Exceptions;

namespace RhSensoWebApi.API.Middleware
{
    public class ExceptionHandlingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ExceptionHandlingMiddleware> _logger;

        public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task Invoke(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                await HandleExceptionAsync(context, ex, _logger);
            }
        }

        private static async Task HandleExceptionAsync(HttpContext ctx, Exception ex, ILogger logger)
        {
            var traceId = ctx.TraceIdentifier;
            var (status, message, code, errors) = MapException(ex);

            logger.LogError(ex, "Unhandled exception. Status: {StatusCode} TraceId: {TraceId} Msg: {Message}",
                (int)status, traceId, message);

            ctx.Response.ContentType = "application/json";
            ctx.Response.StatusCode = (int)status;

            var payload = new BaseResponse<object>
            {
                Success = false,
                Message = message,
                Error = new ErrorDto { Code = code, Message = message },
                Errors = errors,
                TraceId = traceId
            };

            var json = JsonSerializer.Serialize(payload, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = false
            });

            await ctx.Response.WriteAsync(json);
        }

        private static (HttpStatusCode status, string message, string code, IDictionary<string, string[]>? errors) MapException(Exception ex)
        {
            switch (ex)
            {
                case ValidationException vex:
                    return (HttpStatusCode.BadRequest, "Falha de validação.", nameof(HttpStatusCode.BadRequest), vex.Errors);
                case UnauthorizedAccessException uex:
                    return (HttpStatusCode.Unauthorized, string.IsNullOrWhiteSpace(uex.Message) ? "Não autorizado." : uex.Message, nameof(HttpStatusCode.Unauthorized), null);
                case ForbiddenException fex:
                    return (HttpStatusCode.Forbidden, string.IsNullOrWhiteSpace(fex.Message) ? "Acesso proibido." : fex.Message, nameof(HttpStatusCode.Forbidden), null);
                case NotFoundException nex:
                    return (HttpStatusCode.NotFound, string.IsNullOrWhiteSpace(nex.Message) ? "Recurso não encontrado." : nex.Message, nameof(HttpStatusCode.NotFound), null);
                case ConcurrencyException cex:
                    return (HttpStatusCode.Conflict, string.IsNullOrWhiteSpace(cex.Message) ? "Conflito de concorrência." : cex.Message, nameof(HttpStatusCode.Conflict), null);
                case DbUpdateConcurrencyException:
                    return (HttpStatusCode.Conflict, "Conflito de concorrência no banco de dados.", nameof(HttpStatusCode.Conflict), null);
                case DbUpdateException:
                    return (HttpStatusCode.BadRequest, "Falha ao persistir alterações no banco de dados.", nameof(HttpStatusCode.BadRequest), null);
                default:
                    return (HttpStatusCode.InternalServerError, "Erro interno do servidor.", nameof(HttpStatusCode.InternalServerError), null);
            }
        }
    }
}
