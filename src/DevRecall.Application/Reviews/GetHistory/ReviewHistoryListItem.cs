namespace DevRecall.Application.Reviews.GetHistory;

public sealed record ReviewHistoryListItem(
    Guid Id,
    string Evaluation,
    int PreviousIntervalDays,
    int NextIntervalDays,
    DateTimeOffset PreviousDueAtUtc,
    DateTimeOffset NextDueAtUtc,
    DateTimeOffset ReviewedAtUtc,
    DateTimeOffset CreatedAtUtc);
