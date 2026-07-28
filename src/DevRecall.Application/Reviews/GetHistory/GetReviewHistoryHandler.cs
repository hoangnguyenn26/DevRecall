using DevRecall.Application.Common.Exceptions;
using DevRecall.Application.Identity;
using DevRecall.Domain.Reviews;

namespace DevRecall.Application.Reviews.GetHistory;

public sealed class GetReviewHistoryHandler(
    IReviewItemRepository reviewItemRepository,
    IReviewHistoryRepository reviewHistoryRepository,
    ICurrentUser currentUser)
{
    private const int MaximumPageSize = 100;

    public async Task<GetReviewHistoryResult> HandleAsync(
        GetReviewHistoryQuery query, CancellationToken cancellationToken)
    {
        ValidatePagination(query.Page, query.PageSize);
        var item = await reviewItemRepository.GetByIdAndUserIdAsync(
            query.ReviewItemId, GetCurrentUserId(), cancellationToken);
        if (item is null)
        {
            throw new NotFoundException(
                ReviewErrors.ItemNotFound.Code,
                ReviewErrors.ItemNotFound.Message);
        }

        var skip = checked((query.Page - 1) * query.PageSize);
        var history = await reviewHistoryRepository.GetListAsync(
            item.Id, skip, query.PageSize, cancellationToken);
        var totalPages = history.TotalCount == 0
            ? 0
            : (int)Math.Ceiling(history.TotalCount / (double)query.PageSize);
        return new GetReviewHistoryResult(
            history.Items.Select(item => new ReviewHistoryListItem(
                item.Id, item.Evaluation.ToString(),
                item.PreviousIntervalDays, item.NextIntervalDays,
                item.PreviousDueAtUtc, item.NextDueAtUtc,
                item.ReviewedAtUtc, item.CreatedAtUtc)).ToList(),
            query.Page, query.PageSize, history.TotalCount, totalPages);
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
