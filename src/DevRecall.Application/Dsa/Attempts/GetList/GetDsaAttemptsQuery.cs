namespace DevRecall.Application.Dsa.Attempts.GetList;

public sealed record GetDsaAttemptsQuery(
    Guid DsaProblemId,
    string? Result,
    int Page,
    int PageSize);
