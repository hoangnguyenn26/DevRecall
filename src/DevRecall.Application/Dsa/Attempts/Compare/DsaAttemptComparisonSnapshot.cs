namespace DevRecall.Application.Dsa.Attempts.Compare;

public sealed record DsaAttemptComparisonSnapshot(
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
