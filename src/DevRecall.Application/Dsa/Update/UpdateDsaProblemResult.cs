namespace DevRecall.Application.Dsa.Update;

public sealed record UpdateDsaProblemResult(
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
