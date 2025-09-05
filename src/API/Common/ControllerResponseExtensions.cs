using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using RhSensoWebApi.Core.Common.Exceptions;

namespace RhSensoWebApi.API.Common;

public static class ControllerResponseExtensions
{
    // 200 OK padrão com envelope
    public static IActionResult OkResponse<T>(this ControllerBase c, T data, string? message = null)
    {
        var resp = new BaseResponse<T>
        {
            Success = true,
            Message = message,
            Data = data,
            Error = null,
            Errors = null,
            TraceId = c?.HttpContext?.TraceIdentifier ?? Guid.NewGuid().ToString("N"),
            Timestamp = DateTime.UtcNow
        };

        return new ObjectResult(resp) { StatusCode = StatusCodes.Status200OK };
    }

    // Erros gerais (401/403/404/409/500...). Assinatura: (statusCode, message, code?, errors?)
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
            Error = code is null ? null : new ErrorDto { Code = code, Message = message },
            Errors = (errors is not null && errors.Count > 0) ? errors : null,
            TraceId = c?.HttpContext?.TraceIdentifier ?? Guid.NewGuid().ToString("N"),
            Timestamp = DateTime.UtcNow
        };

        return new ObjectResult(resp) { StatusCode = statusCode };
    }

    // Erros de validação (400) — retorna 'error' + 'errors'
    public static IActionResult FailValidation(
        this ControllerBase c,
        IDictionary<string, string[]> errors,
        string? message = null)
    {
        if (errors is null || errors.Count == 0)
            throw new ArgumentException("Validation errors cannot be empty.", nameof(errors));

        var msg = message ?? "Erro de validação";

        var resp = new BaseResponse<object?>
        {
            Success = false,
            Message = msg,
            Data = null,
            Error = new ErrorDto { Code = "VALIDATION_ERROR", Message = msg },
            Errors = errors,
            TraceId = c?.HttpContext?.TraceIdentifier ?? Guid.NewGuid().ToString("N"),
            Timestamp = DateTime.UtcNow
        };

        return new ObjectResult(resp) { StatusCode = StatusCodes.Status400BadRequest };
    }
}
