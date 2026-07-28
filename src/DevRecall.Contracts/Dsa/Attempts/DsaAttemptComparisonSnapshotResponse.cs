namespace DevRecall.Contracts.Dsa.Attempts;

public sealed record DsaAttemptComparisonSnapshotResponse(
    Guid Id,
    int AttemptNumber,
    string Result,
    string? Language,
    string? SolutionCode,
    string? Approach,
    string? TimeComplexity,
    string? SpaceComplexity,
    int DurationMinutes,
    string? Notes,
    DateTimeOffset AttemptedAtUtc,
    DateTimeOffset CreatedAtUtc);
