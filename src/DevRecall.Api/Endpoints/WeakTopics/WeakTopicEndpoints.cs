using DevRecall.Api.Authorization;
using DevRecall.Application.WeakTopics.GetDetail;
using DevRecall.Application.WeakTopics.GetList;
using DevRecall.Application.WeakTopics.Recalculate;
using DevRecall.Application.WeakTopics.RecalculateAll;
using DevRecall.Contracts.Common;
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
        group.MapPost("/recalculate-all", RecalculateAllAsync)
            .WithName("RecalculateAllWeakTopics")
            .Produces<RecalculateAllWeakTopicsResponse>()
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status409Conflict);
        group.MapGet("/", GetListAsync)
            .WithName("GetWeakTopics")
            .Produces<PagedResponse<WeakTopicListItemResponse>>()
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized);
        group.MapGet("/{profileId:guid}", GetDetailAsync)
            .WithName("GetWeakTopicDetail")
            .Produces<WeakTopicDetailResponse>()
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status404NotFound);
        return endpoints;
    }

    private static async Task<IResult> RecalculateAllAsync(
        RecalculateAllWeakTopicsHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(
            new RecalculateAllWeakTopicsCommand(), cancellationToken);
        return Results.Ok(new RecalculateAllWeakTopicsResponse(
            result.CalculatedAtUtc, result.CandidateResources,
            result.CreatedProfiles, result.UpdatedProfiles,
            result.UnchangedProfiles, result.NoneProfiles, result.LowProfiles,
            result.MediumProfiles, result.HighProfiles, result.CriticalProfiles));
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

    private static async Task<IResult> GetListAsync(
        [AsParameters] GetWeakTopicsRequest request, GetWeakTopicsHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new GetWeakTopicsQuery(
            request.Level, request.ResourceType, request.MinimumScore,
            request.IncludeNone, request.Page, request.PageSize), cancellationToken);
        return Results.Ok(new PagedResponse<WeakTopicListItemResponse>(
            result.Items.Select(x => new WeakTopicListItemResponse(
                x.ProfileId, x.ResourceType, x.ResourceId, x.ResourceTitle,
                x.ResourcePreview, x.IsResourceAvailable, x.Score, x.Level,
                x.SignalCount, x.Version, x.CalculatedAtUtc, x.UpdatedAtUtc)).ToArray(),
            result.Page, result.PageSize, result.TotalCount, result.TotalPages));
    }

    private static async Task<IResult> GetDetailAsync(
        Guid profileId, GetWeakTopicDetailHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(
            new GetWeakTopicDetailQuery(profileId), cancellationToken);
        return Results.Ok(new WeakTopicDetailResponse(
            result.ProfileId, result.ResourceType, result.ResourceId,
            result.ResourceTitle, result.ResourcePreview, result.IsResourceAvailable,
            result.Score, result.Level, result.SignalCount, result.Version,
            result.CalculatedAtUtc, result.CreatedAtUtc, result.UpdatedAtUtc,
            result.SignalWindowFromUtc, result.SignalWindowToUtc,
            result.LatestSignalAtUtc, result.SignalGroups.Select(x =>
                new WeakTopicSignalGroupResponse(
                    x.SignalType, x.Count, x.TotalWeightedScore)).ToArray(),
            result.Contributions.Select(x =>
                new WeakTopicSignalContributionResponse(
                    x.SignalType, x.OccurredAtUtc, x.BaseWeight,
                    x.RecencyMultiplier, x.WeightedScore)).ToArray()));
    }
}
