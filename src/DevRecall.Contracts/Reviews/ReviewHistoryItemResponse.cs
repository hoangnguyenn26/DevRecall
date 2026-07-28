namespace DevRecall.Contracts.Reviews;

public sealed record ReviewHistoryItemResponse(
    Guid Id,
    string Evaluation,
    int PreviousIntervalDays,
    int NextIntervalDays,
    DateTimeOffset PreviousDueAtUtc,
    DateTimeOffset NextDueAtUtc,
    DateTimeOffset ReviewedAtUtc,
    DateTimeOffset CreatedAtUtc);
