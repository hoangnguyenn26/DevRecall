using System.Net;
using DevRecall.Api.Middleware;
using DevRecall.Api.Tests.Infrastructure;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;

namespace DevRecall.Api.Tests.Middleware;

[Collection(AuthApiTestSuite.Name)]
public sealed class SecurityHeadersMiddlewareTests(AuthApiFactory factory)
{
    [Fact]
    public async Task InvokeAsync_ShouldSetBrowserSecurityHeaders()
    {
        var context = CreateContext("/");
        var middleware = new SecurityHeadersMiddleware(_ => Task.CompletedTask);

        await middleware.InvokeAsync(context);
        await context.Response.CompleteAsync();

        context.Response.Headers["X-Content-Type-Options"].ToString()
            .Should().Be("nosniff");
        context.Response.Headers["Referrer-Policy"].ToString().Should()
            .Be("strict-origin-when-cross-origin");
        context.Response.Headers["X-Frame-Options"].ToString()
            .Should().Be("DENY");
    }

    [Fact]
    public async Task InvokeAsync_ApiResponseShouldDisableSharedCaching()
    {
        var context = CreateContext("/api/v1/today");
        var middleware = new SecurityHeadersMiddleware(_ => Task.CompletedTask);

        await middleware.InvokeAsync(context);
        await context.Response.CompleteAsync();

        context.Response.Headers.CacheControl.ToString().Should().Be("no-store");
        context.Response.Headers.Pragma.ToString().Should().Be("no-cache");
    }

    [Fact]
    public async Task InvokeAsync_PublicResponseShouldRemainCacheableByPolicy()
    {
        var context = CreateContext("/features");
        var middleware = new SecurityHeadersMiddleware(_ => Task.CompletedTask);

        await middleware.InvokeAsync(context);
        await context.Response.CompleteAsync();

        context.Response.Headers.Should().NotContainKey("Cache-Control");
        context.Response.Headers.Should().NotContainKey("Pragma");
    }

    [Fact]
    public async Task ApiPipeline_ShouldApplySecurityAndPrivateCacheHeaders()
    {
        using var client = factory.CreateClient(
            new WebApplicationFactoryClientOptions
            {
                BaseAddress = new Uri("https://localhost"),
                AllowAutoRedirect = false
            });

        using var response = await client.GetAsync("/api/v1/auth/me");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        response.Headers.CacheControl!.NoStore.Should().BeTrue();
        response.Headers.GetValues("X-Content-Type-Options")
            .Should().ContainSingle("nosniff");
        response.Headers.GetValues("Referrer-Policy")
            .Should().ContainSingle("strict-origin-when-cross-origin");
        response.Headers.GetValues("X-Frame-Options")
            .Should().ContainSingle("DENY");
    }

    private static DefaultHttpContext CreateContext(string path)
    {
        var context = new DefaultHttpContext();
        context.Request.Path = path;
        context.Response.Body = new MemoryStream();
        return context;
    }
}
