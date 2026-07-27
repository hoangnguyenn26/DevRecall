namespace DevRecall.Application.Dsa.GetDetail;

public sealed record GetDsaProblemDetailResult(
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
