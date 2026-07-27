namespace DevRecall.Contracts.Dsa;

public sealed record DsaProblemListItemResponse(
    Guid Id,
    string Title,
    string Difficulty,
    string? Source,
    IReadOnlyList<string> Topics,
    DateTimeOffset UpdatedAtUtc);
