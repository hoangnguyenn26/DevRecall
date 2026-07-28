namespace DevRecall.Application.Dsa.Attempts.GetList;

public sealed record GetDsaAttemptsResult(
    IReadOnlyList<DsaAttemptListItem> Items,
    int Page,
    int PageSize,
    int TotalCount,
    int TotalPages);
