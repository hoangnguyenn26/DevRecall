namespace DevRecall.Application.Reviews.GetHistory;

public sealed record GetReviewHistoryQuery(
    Guid ReviewItemId, int Page, int PageSize);
