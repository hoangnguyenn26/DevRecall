using System.Text.Json;
using DevRecall.Api.ExceptionHandling;
using DevRecall.Application.Common.Exceptions;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;

namespace DevRecall.Api.Tests.ExceptionHandling;

public sealed class GlobalExceptionHandlerTests
{
    [Fact]
    public async Task TryHandleAsync_NotFoundException_WritesProblemDetails()
    {
        var exception = new NotFoundException(
            "TEST_RESOURCE_NOT_FOUND",
            "The requested test resource was not found.");

        using var document = await HandleAsync(exception);
        var root = document.RootElement;

        root.GetProperty("status").GetInt32().Should().Be(StatusCodes.Status404NotFound);
        root.GetProperty("type").GetString().Should()
            .Be("https://devrecall/errors/test_resource_not_found");
        root.GetProperty("title").GetString().Should().Be("Resource not found");
        root.GetProperty("detail").GetString().Should()
            .Be("The requested test resource was not found.");
        root.GetProperty("instance").GetString().Should().Be("/api/v1/test");
        root.GetProperty("errorCode").GetString().Should().Be("TEST_RESOURCE_NOT_FOUND");
        root.GetProperty("traceId").GetString().Should().Be("test-trace-id");
    }

    [Fact]
    public async Task TryHandleAsync_ValidationException_WritesErrors()
    {
        var exception = new ValidationException(
            new Dictionary<string, string[]>
            {
                ["name"] = ["Name is required."]
            });

        using var document = await HandleAsync(exception);
        var root = document.RootElement;

        root.GetProperty("status").GetInt32().Should().Be(StatusCodes.Status400BadRequest);
        root.GetProperty("errorCode").GetString().Should().Be("VALIDATION_FAILED");
        root.GetProperty("errors").GetProperty("name")[0].GetString()
            .Should().Be("Name is required.");
    }

    [Fact]
    public async Task TryHandleAsync_ConflictException_WritesProblemDetails()
    {
        using var document = await HandleAsync(
            new ConflictException(
                "TEST_CONFLICT",
                "The requested operation conflicts with current state."));
        var root = document.RootElement;

        root.GetProperty("status").GetInt32().Should().Be(StatusCodes.Status409Conflict);
        root.GetProperty("title").GetString().Should().Be("Conflict");
        root.GetProperty("errorCode").GetString().Should().Be("TEST_CONFLICT");
        root.GetProperty("traceId").GetString().Should().Be("test-trace-id");
    }

    [Fact]
    public async Task TryHandleAsync_ForbiddenException_WritesProblemDetails()
    {
        using var document = await HandleAsync(
            new ForbiddenException(
                "TEST_FORBIDDEN",
                "The requested operation is forbidden."));
        var root = document.RootElement;

        root.GetProperty("status").GetInt32().Should().Be(StatusCodes.Status403Forbidden);
        root.GetProperty("title").GetString().Should().Be("Forbidden");
        root.GetProperty("errorCode").GetString().Should().Be("TEST_FORBIDDEN");
        root.GetProperty("traceId").GetString().Should().Be("test-trace-id");
    }

    [Fact]
    public async Task TryHandleAsync_UnexpectedException_HidesExceptionDetails()
    {
        const string sensitiveMessage = "Sensitive implementation detail";

        using var document = await HandleAsync(new InvalidOperationException(sensitiveMessage));
        var json = document.RootElement.GetRawText();
        var root = document.RootElement;

        root.GetProperty("status").GetInt32().Should()
            .Be(StatusCodes.Status500InternalServerError);
        root.GetProperty("errorCode").GetString().Should().Be("INTERNAL_SERVER_ERROR");
        root.GetProperty("detail").GetString().Should().Be("An unexpected error occurred.");
        json.Should().NotContain(sensitiveMessage);
        json.Should().NotContain(nameof(InvalidOperationException));
    }

    private static async Task<JsonDocument> HandleAsync(Exception exception)
    {
        var context = new DefaultHttpContext
        {
            TraceIdentifier = "test-trace-id"
        };
        context.Request.Method = HttpMethods.Get;
        context.Request.Path = "/api/v1/test";
        context.Response.Body = new MemoryStream();

        var handler = new GlobalExceptionHandler(
            NullLogger<GlobalExceptionHandler>.Instance);

        var handled = await handler.TryHandleAsync(
            context,
            exception,
            CancellationToken.None);

        handled.Should().BeTrue();
        context.Response.Body.Position = 0;

        return await JsonDocument.ParseAsync(
            context.Response.Body,
            cancellationToken: CancellationToken.None);
    }
}
