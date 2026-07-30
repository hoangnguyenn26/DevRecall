namespace DevRecall.Contracts.Recommendations;

public sealed class GenerateRecommendationsRequest
{
    public int MaximumCandidates { get; init; } = 100;
}

public sealed record GenerateRecommendationsResponse(
    DateTimeOffset GeneratedAtUtc, int CandidateCount,
    int CreatedCount, int UpdatedCount, int UnchangedCount,
    int LowPriorityCount, int MediumPriorityCount,
    int HighPriorityCount, int CriticalPriorityCount);

public sealed class GetRecommendationsRequest
{
    public string? Status { get; init; }
    public string? Priority { get; init; }
    public string? ResourceType { get; init; }
    public string? Type { get; init; }
    public decimal? MinimumPriorityScore { get; init; }
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 20;
}

public sealed record RecommendationListItemResponse(
    Guid RecommendationId, string ResourceType, Guid ResourceId,
    string ResourceTitle, string? ResourcePreview, bool IsResourceAvailable,
    string Type, string Priority, decimal PriorityScore, string Status,
    decimal WeaknessScore, string WeaknessLevel, int SignalCount,
    DateTimeOffset WeaknessCalculatedAtUtc, DateTimeOffset GeneratedAtUtc,
    DateTimeOffset? ExpiresAtUtc, DateTimeOffset? DismissedAtUtc,
    DateTimeOffset? CompletedAtUtc, DateTimeOffset? ExpiredAtUtc, int Version);
