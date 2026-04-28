using System.Diagnostics;

namespace CarAssemblyErp.Middleware;

public class RequestTimingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestTimingMiddleware> _logger;

    public RequestTimingMiddleware(RequestDelegate next, ILogger<RequestTimingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var stopwatch = Stopwatch.StartNew();
        var method = context.Request.Method;
        var path = context.Request.Path;

        try
        {
            await _next(context);
        }
        finally
        {
            stopwatch.Stop();
            var statusCode = context.Response.StatusCode;
            _logger.LogInformation("[{Timestamp:HH:mm:ss} {Level}] {RequestMethod} {RequestPath} => {StatusCode} in {DurationMs}ms",
                DateTime.UtcNow,
                statusCode >= 400 ? "Warning" : "Information",
                method,
                path,
                statusCode,
                stopwatch.ElapsedMilliseconds);
        }
    }
}
