namespace DevRecall.Contracts.Reviews;

public sealed record EvaluateReviewItemResponse(
    Guid ReviewItemId,
    Guid ReviewHistoryId,
    string Evaluation,
    int PreviousIntervalDays,
    int NextIntervalDays,
    DateTimeOffset PreviousDueAtUtc,
    DateTimeOffset ReviewedAtUtc,
    DateTimeOffset NextDueAtUtc,
    int ReviewCount);
