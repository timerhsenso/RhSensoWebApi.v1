using System.Diagnostics;

namespace RhSensoWebApi.API.Middleware;

public class RequestLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestLoggingMiddleware> _logger;
    
    public RequestLoggingMiddleware(RequestDelegate next, ILogger<RequestLoggingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }
    
    public async Task InvokeAsync(HttpContext context)
    {
        var stopwatch = Stopwatch.StartNew();
        
        _logger.LogInformation("Iniciando requisição: {Method} {Path}",
            context.Request.Method, context.Request.Path);
        
        await _next(context);
        
        stopwatch.Stop();
        
        _logger.LogInformation("Requisição finalizada: {Method} {Path} - Status: {StatusCode} - Duração: {Duration}ms",
            context.Request.Method, 
            context.Request.Path, 
            context.Response.StatusCode, 
            stopwatch.ElapsedMilliseconds);
    }
}

