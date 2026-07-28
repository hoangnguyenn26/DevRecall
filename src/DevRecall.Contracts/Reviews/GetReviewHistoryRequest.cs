namespace DevRecall.Contracts.Reviews;

public sealed class GetReviewHistoryRequest
{
    public int? Page { get; init; }
    public int? PageSize { get; init; }
}
