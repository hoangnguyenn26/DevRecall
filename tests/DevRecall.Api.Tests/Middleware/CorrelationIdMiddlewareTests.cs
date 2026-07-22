using System.Text.Json;
using DevRecall.Api.ExceptionHandling;
using DevRecall.Api.Middleware;
using DevRecall.Application.Common.Exceptions;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;

namespace DevRecall.Api.Tests.Middleware;

public sealed class CorrelationIdMiddlewareTests
{
    [Fact]
    public async Task InvokeAsync_WithClientCorrelationId_EchoesItAndSetsTraceIdentifier()
    {
        const string correlationId = "devrecall-local-test-001";
        var context = CreateContext();
        context.Request.Headers[CorrelationIdMiddleware.HeaderName] = correlationId;
        string? observedTraceIdentifier = null;

        var middleware = new CorrelationIdMiddleware(
            next: nextContext =>
            {
                observedTraceIdentifier = nextContext.TraceIdentifier;
                return Task.CompletedTask;
            },
            NullLogger<CorrelationIdMiddleware>.Instance);

        await middleware.InvokeAsync(context);

        observedTraceIdentifier.Should().Be(correlationId);
        context.Response.Headers[CorrelationIdMiddleware.HeaderName].ToString()
            .Should().Be(correlationId);
    }

    [Fact]
    public async Task InvokeAsync_WithoutClientCorrelationId_GeneratesOne()
    {
        var context = CreateContext();
        var middleware = new CorrelationIdMiddleware(
            _ => Task.CompletedTask,
            NullLogger<CorrelationIdMiddleware>.Instance);

        await middleware.InvokeAsync(context);

        context.TraceIdentifier.Should().HaveLength(32);
        Guid.TryParseExact(context.TraceIdentifier, "N", out _).Should().BeTrue();
        context.Response.Headers[CorrelationIdMiddleware.HeaderName].ToString()
            .Should().Be(context.TraceIdentifier);
    }

    [Fact]
    public async Task InvokeAsync_WhenProblemDetailsIsWritten_UsesCorrelationIdAsTraceId()
    {
        const string correlationId = "problem-details-test-001";
        var context = CreateContext();
        context.Request.Path = "/api/v1/test";
        context.Request.Headers[CorrelationIdMiddleware.HeaderName] = correlationId;
        var exceptionHandler = new GlobalExceptionHandler(
            NullLogger<GlobalExceptionHandler>.Instance);
        var middleware = new CorrelationIdMiddleware(
            async nextContext =>
            {
                await exceptionHandler.TryHandleAsync(
                    nextContext,
                    new NotFoundException("RESOURCE_NOT_FOUND", "Resource not found."),
                    CancellationToken.None);
            },
            NullLogger<CorrelationIdMiddleware>.Instance);

        await middleware.InvokeAsync(context);
        context.Response.Body.Position = 0;
        using var document = await JsonDocument.ParseAsync(
            context.Response.Body,
            cancellationToken: CancellationToken.None);

        document.RootElement.GetProperty("traceId").GetString()
            .Should().Be(correlationId);
    }

    private static DefaultHttpContext CreateContext()
    {
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();
        return context;
    }
}
