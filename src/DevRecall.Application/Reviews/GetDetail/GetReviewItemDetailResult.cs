namespace DevRecall.Application.Reviews.GetDetail;

public sealed record GetReviewItemDetailResult(
    Guid Id,
    string Status,
    DateTimeOffset DueAtUtc,
    DateTimeOffset? LastReviewedAtUtc,
    int IntervalDays,
    int ReviewCount,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc,
    ReviewItemDetailResource Resource,
    ReviewSourceProvenance? Source,
    IReadOnlyList<ReviewHistoryOverview> RecentHistory);
