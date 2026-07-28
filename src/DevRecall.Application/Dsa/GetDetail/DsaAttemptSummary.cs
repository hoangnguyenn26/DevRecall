namespace DevRecall.Application.Dsa.GetDetail;

public sealed record DsaAttemptSummary(
    int TotalAttempts,
    int SolvedAttempts,
    int PartiallySolvedAttempts,
    int FailedAttempts,
    int SkippedAttempts,
    int TotalDurationMinutes,
    double AverageDurationMinutes,
    DateTimeOffset? LastAttemptedAtUtc);
