namespace DevRecall.Application.Reviews.Create;

public sealed record CreateReviewItemResult(
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
