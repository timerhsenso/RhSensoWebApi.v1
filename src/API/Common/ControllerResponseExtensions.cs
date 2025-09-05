using Microsoft.AspNetCore.Mvc;
using RhSensoWebApi.Core.Common.Exceptions;

namespace RhSensoWebApi.API.Common;

public static class ControllerResponseExtensions
{
    public static IActionResult OkResponse<T>(this ControllerBase c, T data, string? message = null)
    {
        var resp = new BaseResponse<T>
        {
            Success = true,
            Message = message,
            Data = data,
            Error = null,
            Errors = null,
            TraceId = GetTraceId(c)
        };

        return new ObjectResult(resp) { StatusCode = StatusCodes.Status200OK };
    }

    /// <summary>
    /// Erros gerais (401/403/404/409/500...). Se precisar passar erros por campo, use o parâmetro 'errors'
    /// ou crie uma sobrecarga específica para validação.
    /// </summary>
    public static IActionResult FailResponse(
        this ControllerBase c,
        int statusCode,
        string message,
        string? code = null,
        IDictionary<string, string[]>? errors = null)
    {
        var resp = new BaseResponse<object?>
        {
            Success = false,
            Message = message,
            Data = null,
            // só popula Error se tiver 'code' (boa prática: código de erro curto)
            Error = code is null ? null : new ErrorDto { Code = code, Message = message },
            // só popula Errors quando existir e tiver itens (não fabricar dict vazio)
            Errors = (errors is not null && errors.Count > 0) ? errors : null,
            TraceId = GetTraceId(c)
        };

        return new ObjectResult(resp) { StatusCode = statusCode };
    }

    /// <summary>
    /// Atalho específico para erros de validação (400), evita setar 'Error' e garante 'Errors'.
    /// </summary>
    public static IActionResult FailValidation(
        this ControllerBase c,
        IDictionary<string, string[]> errors,
        string? message = null)
    {
        var resp = new BaseResponse<object?>
        {
            Success = false,
            Message = message ?? "Erro de validação",
            Data = null,
            Error = null,
            Errors = (errors is not null && errors.Count > 0) ? errors : null,
            TraceId = GetTraceId(c)
        };

        return new ObjectResult(resp) { StatusCode = StatusCodes.Status400BadRequest };
    }

    private static string GetTraceId(ControllerBase c)
        => c?.HttpContext?.TraceIdentifier ?? Guid.NewGuid().ToString("N");
}
