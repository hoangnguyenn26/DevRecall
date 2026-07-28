namespace DevRecall.Application.Reviews.GetDue;

public sealed record GetDueReviewItemsResult(
    IReadOnlyList<DueReviewItem> Items,
    int Page,
    int PageSize,
    int TotalCount,
    int TotalPages);
