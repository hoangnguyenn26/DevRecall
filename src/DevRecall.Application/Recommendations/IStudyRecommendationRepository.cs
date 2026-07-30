using DevRecall.Domain.Recommendations;

namespace DevRecall.Application.Recommendations;

public interface IStudyRecommendationRepository
{
    Task<StudyRecommendation?> GetActiveByUserAndResourceForUpdateAsync(
        Guid userId, RecommendationResourceType resourceType, Guid resourceId,
        RecommendationType type, CancellationToken cancellationToken);
    Task<StudyRecommendation?> GetByIdAndUserIdForUpdateAsync(
        Guid recommendationId, Guid userId, CancellationToken cancellationToken);
    void Add(StudyRecommendation recommendation);
    Task SaveChangesAsync(CancellationToken cancellationToken);
}

public sealed class RecommendationPersistenceConflictException(
    string message, Exception innerException) : Exception(message, innerException);

public sealed class ActiveRecommendationAlreadyExistsException(
    string message, Exception innerException) : Exception(message, innerException);
