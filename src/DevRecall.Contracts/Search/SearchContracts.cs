using DevRecall.Contracts.Common;

namespace DevRecall.Contracts.Search;

public sealed class SearchRequest
{
    public string? Q { get; init; }
    public string[]? Modules { get; init; }
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 20;
}

public sealed record SearchResultResponse(
    string ResourceType, Guid ResourceId, string Title,
    string? Preview, double Rank,
    IReadOnlyDictionary<string, string> Metadata);

public static class SearchResponse
{
    public static PagedResponse<SearchResultResponse> Create(
        IReadOnlyList<SearchResultResponse> items, int page, int pageSize,
        int totalCount, int totalPages) =>
        new(items, page, pageSize, totalCount, totalPages);
}
