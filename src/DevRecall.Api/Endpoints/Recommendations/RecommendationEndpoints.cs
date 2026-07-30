using DevRecall.Api.Authorization;
using DevRecall.Application.Recommendations.Generation;
using DevRecall.Application.Recommendations.GetDetail;
using DevRecall.Application.Recommendations.GetList;
using DevRecall.Application.Recommendations.Lifecycle;
using DevRecall.Application.Recommendations.Synchronize;
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
        group.MapPost("/synchronize", SynchronizeAsync)
            .WithName("SynchronizeRecommendations")
            .Produces<SynchronizeRecommendationsResponse>()
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status409Conflict);
        group.MapGet("/", GetListAsync)
            .WithName("GetRecommendations")
            .Produces<PagedResponse<RecommendationListItemResponse>>()
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized);
        group.MapGet("/{recommendationId:guid}", GetDetailAsync)
            .WithName("GetRecommendationDetail")
            .Produces<RecommendationDetailResponse>()
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status404NotFound);
        group.MapPost("/{recommendationId:guid}/dismiss", DismissAsync)
            .WithName("DismissRecommendation")
            .Produces<RecommendationMutationResponse>()
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);
        group.MapPost("/{recommendationId:guid}/complete", CompleteAsync)
            .WithName("CompleteRecommendation")
            .Produces<RecommendationMutationResponse>()
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);
        return endpoints;
    }

    private static async Task<IResult> SynchronizeAsync(
        SynchronizeRecommendationsHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(
            new SynchronizeRecommendationsCommand(), cancellationToken);
        return Results.Ok(new SynchronizeRecommendationsResponse(
            result.SynchronizedAtUtc, result.ActiveRecommendationsChecked,
            result.ExpiredRecommendations, result.LifetimeElapsedCount,
            result.WeaknessResolvedCount, result.ResourceUnavailableCount,
            result.UnchangedRecommendations));
    }

    private static async Task<IResult> GetDetailAsync(
        Guid recommendationId, GetRecommendationDetailHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(
            new GetRecommendationDetailQuery(recommendationId), cancellationToken);
        return Results.Ok(new RecommendationDetailResponse(
            result.RecommendationId, result.ResourceType, result.ResourceId,
            result.ResourceTitle, result.ResourcePreview,
            result.IsResourceAvailable, result.Type, result.Priority,
            result.PriorityScore, result.Status, new RecommendationReasonResponse(
                result.WeaknessScore, result.WeaknessLevel, result.SignalCount,
                result.WeaknessCalculatedAtUtc), result.GeneratedAtUtc,
            result.ExpiresAtUtc, result.DismissedAtUtc, result.CompletedAtUtc,
            result.ExpiredAtUtc, result.ExpirationReason, result.CreatedAtUtc,
            result.UpdatedAtUtc, result.Version));
    }

    private static async Task<IResult> DismissAsync(
        Guid recommendationId, RecommendationMutationRequest request,
        DismissRecommendationHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(
            new DismissRecommendationCommand(
                recommendationId, request.ExpectedVersion), cancellationToken);
        return Results.Ok(MapMutationResponse(result));
    }

    private static async Task<IResult> CompleteAsync(
        Guid recommendationId, RecommendationMutationRequest request,
        CompleteRecommendationHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(
            new CompleteRecommendationCommand(
                recommendationId, request.ExpectedVersion), cancellationToken);
        return Results.Ok(MapMutationResponse(result));
    }

    private static RecommendationMutationResponse MapMutationResponse(
        RecommendationMutationResult result) =>
        new(
            result.RecommendationId, result.Status, result.UpdatedAtUtc,
            result.DismissedAtUtc, result.CompletedAtUtc, result.ExpiredAtUtc,
            result.Version);

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
                x.CompletedAtUtc, x.ExpiredAtUtc, x.ExpirationReason,
                x.Version)).ToArray(),
            result.Page, result.PageSize, result.TotalCount, result.TotalPages));
    }
}
