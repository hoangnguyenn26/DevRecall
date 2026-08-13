using DevRecall.Application.Common.Exceptions;
using DevRecall.Application.Identity;
using DevRecall.Application.Reviews.Resources;
using DevRecall.Domain.Reviews;

namespace DevRecall.Application.Reviews.GetDetail;

public sealed class GetReviewItemDetailHandler(
    IReviewItemRepository reviewItemRepository,
    IReviewHistoryRepository reviewHistoryRepository,
    IReviewResourceSummaryReader resourceSummaryReader,
    ICurrentUser currentUser)
{
    private const int RecentHistoryCount = 5;

    public async Task<GetReviewItemDetailResult> HandleAsync(
        GetReviewItemDetailQuery query, CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        var item = await reviewItemRepository.GetByIdAndUserIdAsync(
            query.ReviewItemId, userId, cancellationToken);
        if (item is null)
        {
            throw new NotFoundException(
                ReviewErrors.ItemNotFound.Code,
                ReviewErrors.ItemNotFound.Message);
        }

        var summaries = await resourceSummaryReader.ReadManyAsync(
            userId,
            [new ReviewResourceReference(item.ResourceType, item.ResourceId)],
            cancellationToken);
        var summary = summaries.SingleOrDefault();
        var source = (await reviewItemRepository.GetLearningContentSourcesAsync(
            userId, [item.Id], cancellationToken)).SingleOrDefault();
        var recent = await reviewHistoryRepository.GetRecentAsync(
            item.Id, RecentHistoryCount, cancellationToken);

        return new GetReviewItemDetailResult(
            item.Id, item.Status.ToString(), item.DueAtUtc,
            item.LastReviewedAtUtc, item.IntervalDays, item.ReviewCount,
            item.CreatedAtUtc, item.UpdatedAtUtc,
            new ReviewItemDetailResource(
                item.ResourceType.ToString(), item.ResourceId,
                summary?.Title ?? "Unavailable resource", summary?.Preview,
                summary is not null),
            source,
            recent.Select(MapHistory).ToList());
    }

    private static ReviewHistoryOverview MapHistory(
        ReviewHistoryReadModel history) =>
        new(
            history.Id, history.Evaluation.ToString(),
            history.PreviousIntervalDays, history.NextIntervalDays,
            history.PreviousDueAtUtc, history.NextDueAtUtc,
            history.ReviewedAtUtc, history.CreatedAtUtc);

    private Guid GetCurrentUserId()
    {
        if (!currentUser.IsAuthenticated || currentUser.UserId is null)
        {
            throw new UnauthorizedException(
                "IDENTITY_UNAUTHENTICATED", "Authentication is required.");
        }

        return currentUser.UserId.Value;
    }
}
