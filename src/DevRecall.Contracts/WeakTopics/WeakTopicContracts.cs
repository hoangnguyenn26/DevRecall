namespace DevRecall.Contracts.WeakTopics;

public sealed record RecalculateWeakTopicRequest(string ResourceType, Guid ResourceId);
public sealed record WeakTopicContributionResponse(
    string SignalType, int Weight, decimal RecencyMultiplier, decimal WeightedScore);
public sealed record RecalculateWeakTopicResponse(
    Guid ProfileId, string ResourceType, Guid ResourceId, string ResourceTitle,
    bool IsResourceAvailable, decimal Score, string Level, int SignalCount,
    int Version, DateTimeOffset CalculatedAtUtc, bool WasCreated,
    IReadOnlyList<WeakTopicContributionResponse> Contributions);
