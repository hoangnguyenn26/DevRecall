namespace DevRecall.Contracts.Reviews;

public sealed record CreateReviewItemResponse(
    Guid Id,
    string ResourceType,
    Guid ResourceId,
    string Status,
    DateTimeOffset DueAtUtc,
    DateTimeOffset? LastReviewedAtUtc,
    int IntervalDays,
    int ReviewCount,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc);
