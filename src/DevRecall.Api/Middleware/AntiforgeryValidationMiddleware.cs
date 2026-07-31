using Microsoft.AspNetCore.Antiforgery;

namespace DevRecall.Api.Middleware;

public sealed class AntiforgeryValidationMiddleware(RequestDelegate next)
{
    private static readonly HashSet<string> SafeMethods =
        new(StringComparer.OrdinalIgnoreCase) { "GET", "HEAD", "OPTIONS", "TRACE" };

    public async Task InvokeAsync(
        HttpContext context, IAntiforgery antiforgery,
        IWebHostEnvironment environment)
    {
        if (!environment.IsEnvironment("Testing")
            && context.Request.Path.StartsWithSegments("/api/v1")
            && !SafeMethods.Contains(context.Request.Method))
        {
            await antiforgery.ValidateRequestAsync(context);
        }
        await next(context);
    }
}
