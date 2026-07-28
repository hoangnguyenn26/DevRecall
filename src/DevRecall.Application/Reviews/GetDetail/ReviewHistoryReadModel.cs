using DevRecall.Domain.Reviews;

namespace DevRecall.Application.Reviews.GetDetail;

public sealed record ReviewHistoryReadModel(
    Guid Id,
    ReviewEvaluation Evaluation,
    int PreviousIntervalDays,
    int NextIntervalDays,
    DateTimeOffset PreviousDueAtUtc,
    DateTimeOffset NextDueAtUtc,
    DateTimeOffset ReviewedAtUtc,
    DateTimeOffset CreatedAtUtc);
