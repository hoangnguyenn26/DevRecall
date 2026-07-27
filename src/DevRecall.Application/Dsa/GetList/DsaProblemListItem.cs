namespace DevRecall.Application.Dsa.GetList;

public sealed record DsaProblemListItem(
    Guid Id,
    string Title,
    string Difficulty,
    string? Source,
    IReadOnlyList<string> Topics,
    DateTimeOffset UpdatedAtUtc);
