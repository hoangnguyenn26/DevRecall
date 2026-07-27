namespace DevRecall.Application.Dsa.GetList;

public sealed record GetDsaProblemsResult(
    IReadOnlyList<DsaProblemListItem> Items,
    int Page,
    int PageSize,
    int TotalCount,
    int TotalPages);
