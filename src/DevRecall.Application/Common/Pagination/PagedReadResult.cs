namespace DevRecall.Application.Common.Pagination;

public sealed record PagedReadResult<T>(
    IReadOnlyList<T> Items,
    int TotalCount);
