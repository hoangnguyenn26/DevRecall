namespace DevRecall.Contracts.Reviews;

public sealed class GetDueReviewItemsRequest
{
    public string? ResourceType { get; init; }
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 20;
}
