namespace DevRecall.Contracts.WeakTopics;

public sealed record RecalculateWeakTopicRequest(string ResourceType, Guid ResourceId);
public sealed record WeakTopicContributionResponse(
    string SignalType, int Weight, decimal RecencyMultiplier, decimal WeightedScore);
public sealed record RecalculateWeakTopicResponse(
    Guid ProfileId, string ResourceType, Guid ResourceId, string ResourceTitle,
    bool IsResourceAvailable, decimal Score, string Level, int SignalCount,
    int Version, DateTimeOffset CalculatedAtUtc, bool WasCreated,
    IReadOnlyList<WeakTopicContributionResponse> Contributions);

public sealed class GetWeakTopicsRequest
{
    public string? Level { get; init; }
    public string? ResourceType { get; init; }
    public decimal? MinimumScore { get; init; }
    public bool? IncludeNone { get; init; }
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 20;
}

public sealed record WeakTopicListItemResponse(
    Guid ProfileId, string ResourceType, Guid ResourceId, string ResourceTitle,
    string? ResourcePreview, bool IsResourceAvailable, decimal Score, string Level,
    int SignalCount, int Version, DateTimeOffset CalculatedAtUtc,
    DateTimeOffset UpdatedAtUtc);

public sealed record WeakTopicSignalContributionResponse(
    string SignalType, DateTimeOffset OccurredAtUtc, int BaseWeight,
    decimal RecencyMultiplier, decimal WeightedScore);
public sealed record WeakTopicSignalGroupResponse(
    string SignalType, int Count, decimal TotalWeightedScore);
public sealed record WeakTopicReasonResponse(string Type, int Count);
public sealed record WeakTopicDetailResponse(
    Guid ProfileId, string ResourceType, Guid ResourceId, string ResourceTitle,
    string? ResourcePreview, bool IsResourceAvailable, decimal Score, string Level,
    int SignalCount, int Version, DateTimeOffset CalculatedAtUtc,
    DateTimeOffset CreatedAtUtc, DateTimeOffset UpdatedAtUtc,
    DateTimeOffset SignalWindowFromUtc, DateTimeOffset SignalWindowToUtc,
    DateTimeOffset? LatestSignalAtUtc,
    IReadOnlyList<WeakTopicReasonResponse> Reasons,
    IReadOnlyList<WeakTopicSignalGroupResponse> SignalGroups,
    IReadOnlyList<WeakTopicSignalContributionResponse> Contributions);

public sealed record RecalculateAllWeakTopicsResponse(
    DateTimeOffset CalculatedAtUtc, int CandidateResources,
    int CreatedProfiles, int UpdatedProfiles, int UnchangedProfiles,
    int NoneProfiles, int LowProfiles, int MediumProfiles,
    int HighProfiles, int CriticalProfiles);
