namespace DevRecall.Application.Reviews.Evaluate;

public sealed record ReviewSubmissionReadModel(
    Guid UserId,
    Guid SubmissionId,
    Guid ReviewItemId,
    Guid ReviewHistoryId,
    string Evaluation,
    int PreviousIntervalDays,
    int NextIntervalDays,
    DateTimeOffset PreviousDueAtUtc,
    DateTimeOffset ReviewedAtUtc,
    DateTimeOffset NextDueAtUtc,
    int ReviewCount);
