using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

// Usa a SUA BaseResponse oficial
using RhSensoWebApi.Core.Common.Exceptions;

namespace RhSensoWebApi.API.Middleware
{
    /// <summary>
    /// Middleware global de exceções que devolve sempre o formato padronizado do seu BaseResponse.
    /// </summary>
    public sealed class ExceptionHandlingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ExceptionHandlingMiddleware> _logger;

        public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task Invoke(HttpContext ctx)
        {
            try
            {
                await _next(ctx);
            }
            catch (Exception ex)
            {
                var (status, message, errors) = MapException(ex);

                // Warning para erros esperados; Error para os demais
                var level =
                    status is HttpStatusCode.BadRequest
                    or HttpStatusCode.Unauthorized
                    or HttpStatusCode.Forbidden
                    or HttpStatusCode.NotFound
                    or HttpStatusCode.Conflict
                    ? LogLevel.Warning
                    : LogLevel.Error;

                _logger.Log(level, ex, "Handled exception mapped to {StatusCode}", (int)status);

                ctx.Response.ContentType = "application/json";
                ctx.Response.StatusCode = (int)status;

                // Usa sua BaseResponse do Core (sem ErrorDto)
                var payload = new BaseResponse<object>
                {
                    Success = false,
                    Message = message,
                    Errors = errors,                   // dicionário por campo (quando houver)
                    TraceId = ctx.TraceIdentifier,      // traceId no corpo
                    Timestamp = DateTime.UtcNow           // seu Timestamp é DateTime
                };

                var json = JsonSerializer.Serialize(payload, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });

                await ctx.Response.WriteAsync(json);
            }
        }

        private static (HttpStatusCode status, string message, Dictionary<string, string[]>? errors)
            MapException(Exception ex)
        {
            // 400 com dicionário de validação (sem depender de FluentValidation)
            if (ex is AppValidationException avx)
                return (HttpStatusCode.BadRequest, avx.Message, avx.Errors);

            if (ex is UnauthorizedAccessException)
                return (HttpStatusCode.Unauthorized, "Acesso não autorizado.", null);

            if (ex is ForbiddenException)
                return (HttpStatusCode.Forbidden, "Acesso proibido.", null);

            if (ex is KeyNotFoundException)
                return (HttpStatusCode.NotFound, "Recurso não encontrado.", null);

            if (ex is DbUpdateConcurrencyException)
                return (HttpStatusCode.Conflict, "Conflito de concorrência ao atualizar o recurso.", null);

            if (ex is DbUpdateException dbex)
            {
                // Regras por SqlException (SQL Server)
                if (dbex.InnerException is SqlException sqlEx)
                {
                    // 2627/2601 → unicidade
                    if (sqlEx.Number == 2627 || sqlEx.Number == 2601)
                        return (HttpStatusCode.Conflict, "Violação de unicidade do recurso.", null);

                    // 547 → FK/Check
                    if (sqlEx.Number == 547)
                        return (HttpStatusCode.BadRequest, "Violação de integridade referencial.", null);
                }

                // Fallback
                return (HttpStatusCode.BadRequest, "Falha ao persistir dados no banco de dados.", null);
            }

            return (HttpStatusCode.InternalServerError, "Erro interno do servidor.", null);
        }
    }

    /// <summary>Exceção leve para validação com dicionário de erros.</summary>
    public sealed class AppValidationException : Exception
    {
        public Dictionary<string, string[]> Errors { get; }

        public AppValidationException(string message, Dictionary<string, string[]> errors)
            : base(message)
        {
            Errors = errors;
        }
    }

    /// <summary>Exceção leve para 403.</summary>
    public sealed class ForbiddenException : Exception
    {
        public ForbiddenException(string? message = null) : base(message ?? "Acesso proibido.") { }
    }
}
