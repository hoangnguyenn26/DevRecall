using DevRecall.Application.Common.Exceptions;
using DevRecall.Application.Common.Time;
using DevRecall.Application.Identity;
using DevRecall.Application.Reviews.Resources;
using DevRecall.Domain.Reviews;

namespace DevRecall.Application.Reviews.GetDue;

public sealed class GetDueReviewItemsHandler(
    IReviewItemRepository reviewItemRepository,
    IReviewResourceSummaryReader resourceSummaryReader,
    ICurrentUser currentUser,
    IUtcClock utcClock)
{
    private const int MaximumPageSize = 50;

    public async Task<GetDueReviewItemsResult> HandleAsync(
        GetDueReviewItemsQuery query, CancellationToken cancellationToken)
    {
        ValidatePagination(query.Page, query.PageSize);
        var userId = GetCurrentUserId();
        ReviewResourceType? resourceType = string.IsNullOrWhiteSpace(query.ResourceType)
            ? null
            : ReviewResourceTypeParser.Parse(query.ResourceType);
        var now = utcClock.UtcNow;
        var skip = checked((query.Page - 1) * query.PageSize);
        var dueItems = await reviewItemRepository.GetDueAsync(
            userId, now, resourceType, skip, query.PageSize, cancellationToken);
        var references = dueItems.Items
            .Select(item => new ReviewResourceReference(
                item.ResourceType, item.ResourceId))
            .ToArray();
        var summaries = await resourceSummaryReader.ReadManyAsync(
            userId, references, cancellationToken);
        var summaryByKey = summaries.ToDictionary(
            summary => (summary.ResourceType, summary.ResourceId));
        var totalPages = dueItems.TotalCount == 0
            ? 0
            : (int)Math.Ceiling(dueItems.TotalCount / (double)query.PageSize);

        var items = dueItems.Items.Select(item =>
        {
            summaryByKey.TryGetValue(
                (item.ResourceType, item.ResourceId), out var summary);
            return new DueReviewItem(
                item.ReviewItemId, item.ResourceType.ToString(),
                item.ResourceId, summary?.Title ?? "Unavailable resource",
                summary?.Preview, item.DueAtUtc, item.LastReviewedAtUtc,
                item.IntervalDays, item.ReviewCount,
                CalculateOverdueMinutes(item.DueAtUtc, now));
        }).ToList();

        return new GetDueReviewItemsResult(
            items, query.Page, query.PageSize, dueItems.TotalCount, totalPages);
    }

    private static int CalculateOverdueMinutes(
        DateTimeOffset dueAtUtc, DateTimeOffset now)
    {
        var minutes = (now - dueAtUtc).TotalMinutes;
        if (minutes <= 0)
        {
            return 0;
        }

        return minutes >= int.MaxValue
            ? int.MaxValue
            : (int)Math.Floor(minutes);
    }

    private static void ValidatePagination(int page, int pageSize)
    {
        var errors = new Dictionary<string, string[]>();
        if (page < 1)
        {
            errors["page"] = ["Page must be greater than or equal to 1."];
        }

        if (pageSize is < 1 or > MaximumPageSize)
        {
            errors["pageSize"] =
                [$"Page size must be between 1 and {MaximumPageSize}."];
        }

        if (errors.Count > 0)
        {
            throw new ValidationException(errors);
        }
    }

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
