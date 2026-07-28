using DevRecall.Domain.Reviews;

namespace DevRecall.Application.Reviews.GetDue;

public sealed record DueReviewItemReadModel(
    Guid ReviewItemId,
    ReviewResourceType ResourceType,
    Guid ResourceId,
    DateTimeOffset DueAtUtc,
    DateTimeOffset? LastReviewedAtUtc,
    int IntervalDays,
    int ReviewCount);
