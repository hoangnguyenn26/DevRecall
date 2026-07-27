namespace DevRecall.Application.Dsa.GetList;

public sealed record GetDsaProblemsQuery(
    string? Difficulty,
    string? Topic,
    string? Source,
    int Page,
    int PageSize);
