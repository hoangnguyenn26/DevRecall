namespace DevRecall.Contracts.Dsa;

public sealed record DsaAttemptOverviewResponse(
    Guid Id,
    int AttemptNumber,
    string Result,
    string? Language,
    string? TimeComplexity,
    string? SpaceComplexity,
    int DurationMinutes,
    DateTimeOffset AttemptedAtUtc);
