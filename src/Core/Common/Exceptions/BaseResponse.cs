namespace RhSensoWebApi.Core.Common.Exceptions;

public class BaseResponse<T>
{
    public bool Success { get; set; }
    public T? Data { get; set; }
    public ErrorDto? Error { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

public class ErrorDto
{
    public string Code { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
}

