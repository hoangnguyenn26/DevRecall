namespace DevRecall.Application.Dsa.Attempts.GetDetail;

public sealed record GetDsaAttemptDetailResult(
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
    DateTimeOffset CreatedAtUtc,
    string? ProblemTitleSnapshot = null,
    string? DifficultySnapshot = null,
    DateTimeOffset? StartedAtUtc = null,
    DateTimeOffset? CompletedAtUtc = null,
    int? DurationSeconds = null);
