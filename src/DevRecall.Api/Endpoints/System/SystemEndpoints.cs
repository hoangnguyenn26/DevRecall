using DevRecall.Contracts.System;

namespace DevRecall.Api.Endpoints.System;

public static class SystemEndpoints
{
    public static IEndpointRouteBuilder MapSystemEndpoints(
        this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints
            .MapGroup("/api/v1/system")
            .WithTags("System");

        group.MapGet("/info", (IWebHostEnvironment environment) =>
        {
            var response = new SystemInfoResponse(
                ApplicationName: "DevRecall",
                Version: "0.1.0",
                Environment: environment.EnvironmentName,
                CurrentTimeUtc: DateTimeOffset.UtcNow);

            return Results.Ok(response);
        });

        return endpoints;
    }
}
