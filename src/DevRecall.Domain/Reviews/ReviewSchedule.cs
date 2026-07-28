namespace DevRecall.Domain.Reviews;

public sealed record ReviewSchedule(
    int PreviousIntervalDays,
    int NextIntervalDays,
    DateTimeOffset PreviousDueAtUtc,
    DateTimeOffset ReviewedAtUtc,
    DateTimeOffset NextDueAtUtc);
