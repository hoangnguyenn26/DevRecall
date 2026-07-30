using DevRecall.Domain.StudyPlans;

namespace DevRecall.Application.StudyPlans;

public interface IStudyPlanRepository
{
    Task<StudyPlan?> GetByIdAndUserIdForUpdateAsync(
        Guid studyPlanId, Guid userId, CancellationToken cancellationToken);

    Task<StudyPlan?> GetDraftByUserIdForUpdateAsync(
        Guid userId, CancellationToken cancellationToken);

    void Add(StudyPlan studyPlan);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}

public sealed class StudyPlanPersistenceConflictException(
    string message, Exception innerException) : Exception(message, innerException);

public sealed class DraftStudyPlanAlreadyExistsException(
    string message, Exception innerException) : Exception(message, innerException);

public sealed class DuplicateStudyPlanResourceException(
    string message, Exception innerException) : Exception(message, innerException);
