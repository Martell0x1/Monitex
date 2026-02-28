namespace SmartHome.Middlewares;
public class RequestLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestLoggingMiddleware> _logger;

    public RequestLoggingMiddleware(
        RequestDelegate next ,
        ILogger<RequestLoggingMiddleware> logger
    )
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        _logger.LogInformation("Incomming Request: {Method}:{Path}",
        context.Request.Method , context.Request.Path);

        await _next(context);

        _logger.LogInformation("Outgoing Request: {StatusCode}",
        context.Response.StatusCode);
    }
}