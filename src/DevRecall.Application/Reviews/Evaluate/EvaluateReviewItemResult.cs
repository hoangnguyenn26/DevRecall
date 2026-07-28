namespace DevRecall.Application.Reviews.Evaluate;

public sealed record EvaluateReviewItemResult(
    Guid ReviewItemId,
    Guid ReviewHistoryId,
    string Evaluation,
    int PreviousIntervalDays,
    int NextIntervalDays,
    DateTimeOffset PreviousDueAtUtc,
    DateTimeOffset ReviewedAtUtc,
    DateTimeOffset NextDueAtUtc,
    int ReviewCount);
