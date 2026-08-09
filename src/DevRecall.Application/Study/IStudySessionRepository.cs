using DevRecall.Domain.Study;

namespace DevRecall.Application.Study;

public sealed class StudySessionPersistenceConflictException(
    string constraintName, Exception innerException)
    : Exception("The study session could not be persisted.", innerException)
{
    public string ConstraintName { get; } = constraintName;
}

public interface IStudySessionRepository
{
    Task<StudySession?> GetByIdAndUserIdAsync(
        Guid id, Guid userId, CancellationToken cancellationToken);

    Task<StudySession?> GetByIdAndUserIdForUpdateAsync(
        Guid id, Guid userId, CancellationToken cancellationToken);

    void Add(StudySession session);
    Task SaveChangesAsync(CancellationToken cancellationToken);
}
