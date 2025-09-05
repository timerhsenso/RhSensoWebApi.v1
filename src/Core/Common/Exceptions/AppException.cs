using System;
using System.Collections.Generic;

namespace RhSensoWebApi.Core.Common.Exceptions;

/// <summary>
/// Exceção base da aplicação com metadados HTTP e código de erro.
/// </summary>
public abstract class AppException : Exception
{
    /// <summary>HTTP status code a ser retornado.</summary>
    public int StatusCode { get; }

    /// <summary>Código curto e estável do erro (ex.: FORBIDDEN, NOT_FOUND).</summary>
    public string ErrorCode { get; }

    /// <summary>Erros por campo (opcional; usado em validação).</summary>
    public IDictionary<string, string[]>? Errors { get; }

    protected AppException(int statusCode, string errorCode, string message, IDictionary<string, string[]>? errors = null)
        : base(message)
    {
        StatusCode = statusCode;
        ErrorCode = errorCode;
        Errors = errors;
    }
}
