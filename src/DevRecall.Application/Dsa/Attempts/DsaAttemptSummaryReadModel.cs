namespace DevRecall.Application.Dsa.Attempts;

public sealed record DsaAttemptSummaryReadModel(
    int TotalAttempts,
    int SolvedAttempts,
    int PartiallySolvedAttempts,
    int FailedAttempts,
    int SkippedAttempts,
    int TotalDurationMinutes,
    DateTimeOffset? LastAttemptedAtUtc);
