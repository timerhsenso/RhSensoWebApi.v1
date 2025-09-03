namespace RhSensoWebApi.Core.Common.Exceptions;

// (Opcional futuro: mover para RhSensoWebApi.Core.Common.Responses)

public class BaseResponse<T>
{
    public bool Success { get; set; }
    public string? Message { get; set; }                            // NOVO
    public T? Data { get; set; }

    // Para erros �gen�ricos� (n�o-por-campo), mantendo retrocompatibilidade:
    public ErrorDto? Error { get; set; }                            // EXISTENTE

    // Para erros de valida��o (por-campo). Use null quando n�o for valida��o:
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
