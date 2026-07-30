using DevRecall.Api.Middleware;
using DevRecall.Application.Common.Exceptions;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace DevRecall.Api.ExceptionHandling;

public sealed partial class GlobalExceptionHandler(
    ILogger<GlobalExceptionHandler> logger)
    : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        if (exception is AppException appException)
        {
            LogApplicationException(
                logger,
                appException.ErrorCode,
                httpContext.Request.Method,
                httpContext.Request.Path,
                exception);
        }
        else
        {
            LogUnhandledException(
                logger,
                httpContext.Request.Method,
                httpContext.Request.Path,
                exception);
        }

        var problemDetails = MapException(httpContext, exception);

        httpContext.Response.StatusCode =
            problemDetails.Status
            ?? StatusCodes.Status500InternalServerError;
        httpContext.Response.Headers[CorrelationIdMiddleware.HeaderName] =
            httpContext.TraceIdentifier;

        await httpContext.Response.WriteAsJsonAsync(
            problemDetails,
            cancellationToken);

        return true;
    }

    private static ProblemDetails MapException(
        HttpContext context,
        Exception exception)
    {
        return exception switch
        {
            ValidationException validationException =>
                CreateValidationProblemDetails(context, validationException),

            NotFoundException notFoundException =>
                CreateProblemDetails(
                    context,
                    StatusCodes.Status404NotFound,
                    "Resource not found",
                    notFoundException.ErrorCode,
                    notFoundException.Message),

            ConflictException conflictException =>
                CreateProblemDetails(
                    context,
                    StatusCodes.Status409Conflict,
                    "Conflict",
                    conflictException.ErrorCode,
                    conflictException.Message),

            UnprocessableEntityException unprocessableEntityException =>
                CreateProblemDetails(
                    context,
                    StatusCodes.Status422UnprocessableEntity,
                    "Unprocessable entity",
                    unprocessableEntityException.ErrorCode,
                    unprocessableEntityException.Message),

            UnauthorizedException unauthorizedException =>
                CreateProblemDetails(
                    context,
                    StatusCodes.Status401Unauthorized,
                    "Unauthorized",
                    unauthorizedException.ErrorCode,
                    unauthorizedException.Message),

            ForbiddenException forbiddenException =>
                CreateProblemDetails(
                    context,
                    StatusCodes.Status403Forbidden,
                    "Forbidden",
                    forbiddenException.ErrorCode,
                    forbiddenException.Message),

            _ =>
                CreateProblemDetails(
                    context,
                    StatusCodes.Status500InternalServerError,
                    "Internal server error",
                    "INTERNAL_SERVER_ERROR",
                    "An unexpected error occurred.")
        };
    }

    private static ProblemDetails CreateProblemDetails(
        HttpContext context,
        int status,
        string title,
        string errorCode,
        string detail)
    {
        var problemDetails = new ProblemDetails
        {
            Status = status,
            Title = title,
            Detail = detail,
            Instance = context.Request.Path,
            Type = $"https://devrecall/errors/{errorCode.ToLowerInvariant()}"
        };

        problemDetails.Extensions["errorCode"] = errorCode;
        problemDetails.Extensions["traceId"] = context.TraceIdentifier;

        return problemDetails;
    }

    private static ProblemDetails CreateValidationProblemDetails(
        HttpContext context,
        ValidationException exception)
    {
        var problemDetails = CreateProblemDetails(
            context,
            StatusCodes.Status400BadRequest,
            "Validation failed",
            exception.ErrorCode,
            exception.Message);

        problemDetails.Extensions["errors"] = exception.Errors;

        return problemDetails;
    }

    [LoggerMessage(
        EventId = 1,
        Level = LogLevel.Error,
        Message = "Unhandled exception occurred while processing request {Method} {Path}")]
    private static partial void LogUnhandledException(
        ILogger logger,
        string method,
        PathString path,
        Exception exception);

    [LoggerMessage(
        EventId = 2,
        Level = LogLevel.Warning,
        Message = "Application exception {ErrorCode} occurred while processing {Method} {Path}")]
    private static partial void LogApplicationException(
        ILogger logger,
        string errorCode,
        string method,
        PathString path,
        Exception exception);
}
