namespace DevRecall.Application.Reviews.GetDue;

public sealed record GetDueReviewItemsQuery(
    string? ResourceType, int Page, int PageSize);
