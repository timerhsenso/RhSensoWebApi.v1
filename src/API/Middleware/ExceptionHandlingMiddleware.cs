using System.Net;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using RhSensoWebApi.Core.Common.Exceptions; // BaseResponse<T>, ErrorDto e Exceptions

namespace RhSensoWebApi.API.Middleware
{
    /// <summary>
    /// Captura exceções e retorna BaseResponse<object> com status coerente.
    /// </summary>
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
            var (status, message, code) = MapException(ex);

            logger.LogError(ex, "Unhandled exception. Status: {StatusCode} TraceId: {TraceId} Msg: {Message}",
                (int)status, traceId, message);

            ctx.Response.ContentType = "application/json";
            ctx.Response.StatusCode = (int)status;

            var payload = new BaseResponse<object>
            {
                Success = false,
                Error = new ErrorDto
                {
                    Code = code,
                    Message = message
                }
            };

            var json = JsonSerializer.Serialize(payload, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = false
            });

            await ctx.Response.WriteAsync(json);
        }

        private static (HttpStatusCode status, string message, string code) MapException(Exception ex)
        {
            return ex switch
            {
                ValidationException vex              => (HttpStatusCode.BadRequest, vex.Message, nameof(HttpStatusCode.BadRequest)),
                UnauthorizedAccessException uex      => (HttpStatusCode.Unauthorized, uex.Message, nameof(HttpStatusCode.Unauthorized)),
                ForbiddenException fex               => (HttpStatusCode.Forbidden, fex.Message, nameof(HttpStatusCode.Forbidden)),
                NotFoundException nex                => (HttpStatusCode.NotFound, nex.Message, nameof(HttpStatusCode.NotFound)),
                ConcurrencyException cex             => (HttpStatusCode.Conflict, cex.Message, nameof(HttpStatusCode.Conflict)),
                DbUpdateConcurrencyException         => (HttpStatusCode.Conflict, "Conflito de concorrência no banco de dados.", nameof(HttpStatusCode.Conflict)),
                DbUpdateException                    => (HttpStatusCode.BadRequest, "Falha ao persistir alterações no banco de dados.", nameof(HttpStatusCode.BadRequest)),
                _                                    => (HttpStatusCode.InternalServerError, "Erro interno do servidor.", nameof(HttpStatusCode.InternalServerError))
            };
        }
    }
}
