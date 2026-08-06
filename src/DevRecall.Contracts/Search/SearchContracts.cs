namespace DevRecall.Contracts.Search;

public sealed class SearchRequest
{
    public string? Q { get; init; }
    public int TakePerType { get; init; } = 5;
}

public sealed record SearchHighlightResponse(string Field, string Text);
public sealed record GlobalSearchResultResponse(
    Guid ResourceId, string ResourceType, string Title, string? Summary,
    string TargetPath, decimal Rank, DateTimeOffset UpdatedAtUtc,
    IReadOnlyList<SearchHighlightResponse> Highlights);
public sealed record GlobalSearchResponse(
    string Query, IReadOnlyList<GlobalSearchResultResponse> Results, bool HasMore);
