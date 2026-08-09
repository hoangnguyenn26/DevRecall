namespace DevRecall.Api.Middleware;

public sealed class SecurityHeadersMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context)
    {
        var headers = context.Response.Headers;
        headers.TryAdd("X-Content-Type-Options", "nosniff");
        headers.TryAdd("Referrer-Policy", "strict-origin-when-cross-origin");
        headers.TryAdd("X-Frame-Options", "DENY");

        if (context.Request.Path.StartsWithSegments("/api"))
        {
            headers.CacheControl = "no-store";
            headers.Pragma = "no-cache";
        }

        await next(context);
    }
}
