namespace DevRecall.Application.Dsa.Attempts.Create;

public sealed record CreateDsaAttemptResult(
    Guid Id,
    Guid DsaProblemId,
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
