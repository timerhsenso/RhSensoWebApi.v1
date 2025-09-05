using System;

namespace RhSensoWebApi.Core.Common.Exceptions;

/// <summary>403 Forbidden.</summary>
public sealed class ForbiddenException : AppException
{
    public ForbiddenException(string message = "Acesso negado", string errorCode = "FORBIDDEN")
        : base(403, errorCode, message)
    { }
}
