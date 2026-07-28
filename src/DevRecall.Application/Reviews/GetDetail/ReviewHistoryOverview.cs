namespace DevRecall.Application.Reviews.GetDetail;

public sealed record ReviewHistoryOverview(
    Guid Id,
    string Evaluation,
    int PreviousIntervalDays,
    int NextIntervalDays,
    DateTimeOffset PreviousDueAtUtc,
    DateTimeOffset NextDueAtUtc,
    DateTimeOffset ReviewedAtUtc,
    DateTimeOffset CreatedAtUtc);
