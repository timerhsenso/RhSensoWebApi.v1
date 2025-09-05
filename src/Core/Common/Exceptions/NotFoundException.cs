using System;

namespace RhSensoWebApi.Core.Common.Exceptions;

/// <summary>404 Not Found.</summary>
public sealed class NotFoundException : AppException
{
    public NotFoundException(string message = "Não encontrado", string errorCode = "NOT_FOUND")
        : base(404, errorCode, message)
    { }
}
