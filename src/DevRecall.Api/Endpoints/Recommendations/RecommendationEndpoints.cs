using DevRecall.Api.Authorization;
using DevRecall.Application.Recommendations.Generation;
using DevRecall.Application.Recommendations.GetList;
using DevRecall.Contracts.Common;
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
        group.MapGet("/", GetListAsync)
            .WithName("GetRecommendations")
            .Produces<PagedResponse<RecommendationListItemResponse>>()
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized);
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

    private static async Task<IResult> GetListAsync(
        [AsParameters] GetRecommendationsRequest request,
        GetRecommendationsHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new GetRecommendationsQuery(
            request.Status, request.Priority, request.ResourceType, request.Type,
            request.MinimumPriorityScore, request.Page, request.PageSize),
            cancellationToken);
        return Results.Ok(new PagedResponse<RecommendationListItemResponse>(
            result.Items.Select(x => new RecommendationListItemResponse(
                x.RecommendationId, x.ResourceType, x.ResourceId,
                x.ResourceTitle, x.ResourcePreview, x.IsResourceAvailable,
                x.Type, x.Priority, x.PriorityScore, x.Status, x.WeaknessScore,
                x.WeaknessLevel, x.SignalCount, x.WeaknessCalculatedAtUtc,
                x.GeneratedAtUtc, x.ExpiresAtUtc, x.DismissedAtUtc,
                x.CompletedAtUtc, x.ExpiredAtUtc, x.Version)).ToArray(),
            result.Page, result.PageSize, result.TotalCount, result.TotalPages));
    }
}
