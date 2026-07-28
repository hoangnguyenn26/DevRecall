using DevRecall.Api.Authorization;
using DevRecall.Application.Reviews.Create;
using DevRecall.Application.Reviews.GetDue;
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
                request.ResourceType, request.Page, request.PageSize),
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
}
