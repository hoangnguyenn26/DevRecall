using DevRecall.Api.Authorization;
using DevRecall.Application.Reviews.Create;
using DevRecall.Application.Reviews.Evaluate;
using DevRecall.Application.Reviews.GetDetail;
using DevRecall.Application.Reviews.GetDue;
using DevRecall.Application.Reviews.GetHistory;
using DevRecall.Contracts.Common;
using DevRecall.Contracts.Reviews;

namespace DevRecall.Api.Endpoints.Reviews;

public static class ReviewItemEndpoints
{
    public static IEndpointRouteBuilder MapReviewItemEndpoints(
        this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints
            .MapGroup("/api/v1/review-items")
            .WithTags("Review Items")
            .RequireAuthorization(AuthorizationPolicies.AuthenticatedUser);

        group.MapGet("/due", GetDueAsync);
        group.MapGet("/{id:guid}", GetDetailAsync);
        group.MapGet("/{id:guid}/history", GetHistoryAsync);
        group.MapPost("/{id:guid}/evaluate", EvaluateAsync);
        group.MapPost("/", CreateAsync);
        return endpoints;
    }

    private static async Task<IResult> CreateAsync(
        CreateReviewItemRequest request,
        CreateReviewItemHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(
            new CreateReviewItemCommand(
                request.ResourceType, request.ResourceId),
            cancellationToken);
        return Results.Created(
            $"/api/v1/review-items/{result.Id}",
            new CreateReviewItemResponse(
                result.Id, result.ResourceType, result.ResourceId,
                result.Status, result.DueAtUtc, result.LastReviewedAtUtc,
                result.IntervalDays, result.ReviewCount, result.CreatedAtUtc,
                result.UpdatedAtUtc));
    }

    private static async Task<IResult> GetDueAsync(
        [AsParameters] GetDueReviewItemsRequest request,
        GetDueReviewItemsHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(
            new GetDueReviewItemsQuery(
                request.ResourceType, request.Page ?? 1,
                request.PageSize ?? 20),
            cancellationToken);
        return Results.Ok(
            new PagedResponse<DueReviewItemResponse>(
                result.Items.Select(item => new DueReviewItemResponse(
                    item.ReviewItemId, item.ResourceType, item.ResourceId,
                    item.ResourceTitle, item.ResourcePreview, item.DueAtUtc,
                    item.LastReviewedAtUtc, item.IntervalDays, item.ReviewCount,
                    item.OverdueMinutes)).ToList(),
                result.Page, result.PageSize, result.TotalCount,
                result.TotalPages));
    }

    private static async Task<IResult> EvaluateAsync(
        Guid id,
        EvaluateReviewItemRequest request,
        EvaluateReviewItemHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(
            new EvaluateReviewItemCommand(
                id, request.Evaluation, request.ExpectedReviewCount),
            cancellationToken);
        return Results.Ok(new EvaluateReviewItemResponse(
            result.ReviewItemId, result.ReviewHistoryId, result.Evaluation,
            result.PreviousIntervalDays, result.NextIntervalDays,
            result.PreviousDueAtUtc, result.ReviewedAtUtc,
            result.NextDueAtUtc, result.ReviewCount));
    }

    private static async Task<IResult> GetDetailAsync(
        Guid id,
        GetReviewItemDetailHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(
            new GetReviewItemDetailQuery(id), cancellationToken);
        return Results.Ok(new ReviewItemDetailResponse(
            result.Id, result.Status, result.DueAtUtc,
            result.LastReviewedAtUtc, result.IntervalDays, result.ReviewCount,
            result.CreatedAtUtc, result.UpdatedAtUtc,
            new ReviewResourceSummaryResponse(
                result.Resource.ResourceType, result.Resource.ResourceId,
                result.Resource.Title, result.Resource.Preview,
                result.Resource.IsAvailable),
            result.RecentHistory.Select(MapHistory).ToList()));
    }

    private static async Task<IResult> GetHistoryAsync(
        Guid id,
        [AsParameters] GetReviewHistoryRequest request,
        GetReviewHistoryHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(
            new GetReviewHistoryQuery(
                id, request.Page ?? 1, request.PageSize ?? 20),
            cancellationToken);
        return Results.Ok(new PagedResponse<ReviewHistoryItemResponse>(
            result.Items.Select(history => new ReviewHistoryItemResponse(
                history.Id, history.Evaluation,
                history.PreviousIntervalDays, history.NextIntervalDays,
                history.PreviousDueAtUtc, history.NextDueAtUtc,
                history.ReviewedAtUtc, history.CreatedAtUtc)).ToList(),
            result.Page, result.PageSize, result.TotalCount,
            result.TotalPages));
    }

    private static ReviewHistoryItemResponse MapHistory(
        ReviewHistoryOverview history) =>
        new(
            history.Id, history.Evaluation, history.PreviousIntervalDays,
            history.NextIntervalDays, history.PreviousDueAtUtc,
            history.NextDueAtUtc, history.ReviewedAtUtc,
            history.CreatedAtUtc);
}
