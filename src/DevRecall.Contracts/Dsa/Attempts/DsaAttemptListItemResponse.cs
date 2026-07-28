namespace DevRecall.Contracts.Dsa.Attempts;

public sealed record DsaAttemptListItemResponse(
    Guid Id,
    int AttemptNumber,
    string Result,
    string? Language,
    string? TimeComplexity,
    string? SpaceComplexity,
    int DurationMinutes,
    DateTimeOffset AttemptedAtUtc,
    DateTimeOffset CreatedAtUtc);
