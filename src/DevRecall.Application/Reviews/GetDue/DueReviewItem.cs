namespace DevRecall.Application.Reviews.GetDue;

public sealed record DueReviewItem(
    Guid ReviewItemId,
    string ResourceType,
    Guid ResourceId,
    string ResourceTitle,
    string? ResourcePreview,
    DateTimeOffset DueAtUtc,
    DateTimeOffset? LastReviewedAtUtc,
    int IntervalDays,
    int ReviewCount,
    int OverdueMinutes);
