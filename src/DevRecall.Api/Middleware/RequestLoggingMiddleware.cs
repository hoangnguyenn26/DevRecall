using System.Diagnostics;

namespace DevRecall.Api.Middleware;

public sealed partial class RequestLoggingMiddleware(
    RequestDelegate next,
    ILogger<RequestLoggingMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        var stopwatch = Stopwatch.StartNew();

        try
        {
            await next(context);
        }
        finally
        {
            stopwatch.Stop();

            LogRequestCompleted(
                logger,
                context.Request.Method,
                context.Request.Path,
                context.Response.StatusCode,
                stopwatch.ElapsedMilliseconds);
        }
    }

    [LoggerMessage(
        EventId = 2,
        Level = LogLevel.Information,
        Message = "HTTP {Method} {Path} responded {StatusCode} in {ElapsedMilliseconds} ms")]
    private static partial void LogRequestCompleted(
        ILogger logger,
        string method,
        PathString path,
        int statusCode,
        long elapsedMilliseconds);
}
