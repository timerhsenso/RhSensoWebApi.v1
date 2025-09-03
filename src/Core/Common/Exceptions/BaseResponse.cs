namespace RhSensoWebApi.Core.Common.Exceptions;

// (Opcional futuro: mover para RhSensoWebApi.Core.Common.Responses)

public class BaseResponse<T>
{
    public bool Success { get; set; }
    public string? Message { get; set; }                            // NOVO
    public T? Data { get; set; }

    // Para erros “genéricos” (não-por-campo), mantendo retrocompatibilidade:
    public ErrorDto? Error { get; set; }                            // EXISTENTE

    // Para erros de validação (por-campo). Use null quando não for validação:
    public IDictionary<string, string[]>? Errors { get; set; }      // NOVO

    // Rastreabilidade ponta-a-ponta:
    public string TraceId { get; set; } = string.Empty;             // NOVO

    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

public class ErrorDto
{
    public string Code { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
}
