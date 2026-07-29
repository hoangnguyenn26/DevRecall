namespace DevRecall.Contracts.Analytics;

public sealed record DsaPerformanceResponse(
    DateTimeOffset FromUtc, DateTimeOffset ToUtc,
    int TotalAttempts, int SolvedAttempts,
    int PartiallySolvedAttempts, int FailedAttempts,
    int SkippedAttempts, int ProblemsPracticed,
    decimal SolvedRate, decimal AverageDurationMinutes);
