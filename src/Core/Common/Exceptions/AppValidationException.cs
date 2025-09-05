namespace RhSensoWebApi.Core.Common.Exceptions;

/// <summary>400 Bad Request com erros por campo.</summary>
public sealed class AppValidationException : AppException
{
    public AppValidationException(IDictionary<string, string[]> errors, string? message = null)
        : base(400, "VALIDATION_ERROR", message ?? "Erro de validação", errors)
    {
        if (errors is null || errors.Count == 0)
            throw new ArgumentException("Validation errors cannot be empty.", nameof(errors));
    }
}
