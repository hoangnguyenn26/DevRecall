using DevRecall.Domain.WeakTopics;

namespace DevRecall.Application.Recommendations.Generation;

public sealed record RecommendationCandidate(
    Guid WeakTopicProfileId, WeakTopicResourceType ResourceType,
    Guid ResourceId, decimal WeaknessScore, WeaknessLevel WeaknessLevel,
    int SignalCount, DateTimeOffset WeaknessCalculatedAtUtc);

public interface IRecommendationCandidateReader
{
    Task<IReadOnlyList<RecommendationCandidate>> ReadAsync(
        Guid userId, int take, CancellationToken cancellationToken);
}
