namespace DevRecall.Application.Reviews.GetHistory;

public sealed record GetReviewHistoryResult(
    IReadOnlyList<ReviewHistoryListItem> Items,
    int Page,
    int PageSize,
    int TotalCount,
    int TotalPages);
