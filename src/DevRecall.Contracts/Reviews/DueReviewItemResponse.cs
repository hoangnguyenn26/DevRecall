namespace DevRecall.Contracts.Reviews;

public sealed record DueReviewItemResponse(
    Guid ReviewItemId,
    string ResourceType,
    Guid ResourceId,
    string ResourceTitle,
    string? ResourcePreview,
    DateTimeOffset DueAtUtc,
    DateTimeOffset? LastReviewedAtUtc,
    int IntervalDays,
    int ReviewCount,
    int OverdueMinutes,
    ReviewSourceResponse? Source);
