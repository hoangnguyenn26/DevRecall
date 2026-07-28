namespace DevRecall.Contracts.Dsa;

public sealed record DsaAttemptSummaryResponse(
    int TotalAttempts,
    int SolvedAttempts,
    int PartiallySolvedAttempts,
    int FailedAttempts,
    int SkippedAttempts,
    int TotalDurationMinutes,
    double AverageDurationMinutes,
    DateTimeOffset? LastAttemptedAtUtc);
