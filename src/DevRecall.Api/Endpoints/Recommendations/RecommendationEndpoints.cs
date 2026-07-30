using DevRecall.Api.Authorization;
using DevRecall.Application.Recommendations.Generation;
using DevRecall.Contracts.Recommendations;

namespace DevRecall.Api.Endpoints.Recommendations;

public static class RecommendationEndpoints
{
    public static IEndpointRouteBuilder MapRecommendationEndpoints(
        this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/v1/recommendations")
            .WithTags("Recommendations")
            .RequireAuthorization(AuthorizationPolicies.AuthenticatedUser);
        group.MapPost("/generate", GenerateAsync)
            .WithName("GenerateRecommendations")
            .Produces<GenerateRecommendationsResponse>()
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status409Conflict);
        return endpoints;
    }

    private static async Task<IResult> GenerateAsync(
        GenerateRecommendationsRequest request,
        GenerateRecommendationsHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(
            new GenerateRecommendationsCommand(request.MaximumCandidates),
            cancellationToken);
        return Results.Ok(new GenerateRecommendationsResponse(
            result.GeneratedAtUtc, result.CandidateCount,
            result.CreatedCount, result.UpdatedCount, result.UnchangedCount,
            result.LowPriorityCount, result.MediumPriorityCount,
            result.HighPriorityCount, result.CriticalPriorityCount));
    }
}
