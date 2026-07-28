using DevRecall.Domain.Study;

namespace DevRecall.Application.Study;

public interface IStudySessionRepository
{
    Task<StudySession?> GetByIdAndUserIdAsync(
        Guid id, Guid userId, CancellationToken cancellationToken);

    Task<StudySession?> GetByIdAndUserIdForUpdateAsync(
        Guid id, Guid userId, CancellationToken cancellationToken);

    void Add(StudySession session);
    Task SaveChangesAsync(CancellationToken cancellationToken);
}
