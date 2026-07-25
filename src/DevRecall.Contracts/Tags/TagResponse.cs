namespace DevRecall.Contracts.Tags;

public sealed record TagResponse(
    Guid Id,
    string Name,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc);
