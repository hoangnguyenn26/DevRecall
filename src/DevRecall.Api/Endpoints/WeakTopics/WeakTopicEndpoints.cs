using DevRecall.Api.Authorization;
using DevRecall.Application.WeakTopics.Recalculate;
using DevRecall.Contracts.WeakTopics;

namespace DevRecall.Api.Endpoints.WeakTopics;

public static class WeakTopicEndpoints
{
    public static IEndpointRouteBuilder MapWeakTopicEndpoints(
        this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/v1/weak-topics")
            .WithTags("Weak Topics")
            .RequireAuthorization(AuthorizationPolicies.AuthenticatedUser);
        group.MapPost("/recalculate", RecalculateAsync)
            .WithName("RecalculateWeakTopic")
            .Produces<RecalculateWeakTopicResponse>()
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);
        return endpoints;
    }

    private static async Task<IResult> RecalculateAsync(
        RecalculateWeakTopicRequest request, RecalculateWeakTopicHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(
            new RecalculateWeakTopicCommand(request.ResourceType, request.ResourceId),
            cancellationToken);
        return Results.Ok(new RecalculateWeakTopicResponse(
            result.ProfileId, result.ResourceType, result.ResourceId,
            result.ResourceTitle, result.IsResourceAvailable, result.Score,
            result.Level, result.SignalCount, result.Version, result.CalculatedAtUtc,
            result.WasCreated, result.Contributions.Select(x =>
                new WeakTopicContributionResponse(
                    x.SignalType, x.Weight, x.RecencyMultiplier,
                    x.WeightedScore)).ToArray()));
    }
}
