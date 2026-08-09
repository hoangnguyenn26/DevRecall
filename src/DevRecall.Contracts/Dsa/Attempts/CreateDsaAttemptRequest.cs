namespace DevRecall.Contracts.Dsa.Attempts;

public sealed record CreateDsaAttemptRequest(
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
