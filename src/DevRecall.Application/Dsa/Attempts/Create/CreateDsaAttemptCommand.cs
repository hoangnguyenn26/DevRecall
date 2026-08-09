namespace DevRecall.Application.Dsa.Attempts.Create;

public sealed record CreateDsaAttemptCommand(
    Guid DsaProblemId,
    string Result,
    string? Language,
    string? SolutionCode,
    string? Approach,
    string? TimeComplexity,
    string? SpaceComplexity,
    int DurationMinutes,
    string? Notes,
    DateTimeOffset AttemptedAtUtc,
    Guid? SubmissionId = null,
    DateTimeOffset? StartedAtUtc = null);
