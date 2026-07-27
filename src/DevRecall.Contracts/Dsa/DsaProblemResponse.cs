namespace DevRecall.Contracts.Dsa;

public sealed record DsaProblemResponse(
    Guid Id,
    string Title,
    string Description,
    string Difficulty,
    string? Source,
    string? ExternalUrl,
    IReadOnlyList<string> Topics,
    string Status,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc);
