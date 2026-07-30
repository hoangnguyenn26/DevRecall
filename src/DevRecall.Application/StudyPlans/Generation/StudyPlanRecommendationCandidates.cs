using DevRecall.Domain.Recommendations;

namespace DevRecall.Application.StudyPlans.Generation;

public sealed record StudyPlanRecommendationCandidate(
    Guid RecommendationId,
    RecommendationResourceType ResourceType,
    Guid ResourceId,
    RecommendationType RecommendationType,
    RecommendationPriority Priority,
    decimal PriorityScore,
    DateTimeOffset GeneratedAtUtc,
    DateTimeOffset? ExpiresAtUtc,
    int Version);

public interface IStudyPlanRecommendationCandidateReader
{
    Task<IReadOnlyList<StudyPlanRecommendationCandidate>> ReadAsync(
        Guid userId, int take, DateTimeOffset currentUtc,
        CancellationToken cancellationToken);
}
