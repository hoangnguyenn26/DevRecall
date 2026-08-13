namespace DevRecall.Contracts.Reviews;

public sealed record ReviewItemDetailResponse(
    Guid Id,
    string Status,
    DateTimeOffset DueAtUtc,
    DateTimeOffset? LastReviewedAtUtc,
    int IntervalDays,
    int ReviewCount,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc,
    ReviewResourceSummaryResponse Resource,
    ReviewSourceResponse? Source,
    IReadOnlyList<ReviewHistoryItemResponse> RecentHistory);
