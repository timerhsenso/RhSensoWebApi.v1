using System.Text.Json.Serialization;

namespace RhSensoWebApi.Core.Common.Exceptions;

public class BaseResponse<T>
{
    public bool Success { get; set; }
    public string? Message { get; set; }
    public T? Data { get; set; }

    // Erro "geral" (401/403/404/409/500). Omitir quando nulo.
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public ErrorDto? Error { get; set; }

    // Erros de validação por campo (400). Omitir quando nulo.
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IDictionary<string, string[]>? Errors { get; set; }

    // Rastreabilidade
    public string TraceId { get; set; } = string.Empty;

    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

public class ErrorDto
{
    public string Code { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
}
