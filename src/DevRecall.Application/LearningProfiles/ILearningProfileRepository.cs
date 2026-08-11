using DevRecall.Domain.LearningProfiles;

namespace DevRecall.Application.LearningProfiles;

public interface ILearningProfileRepository
{
    Task<LearningProfile?> GetAsync(Guid userId, CancellationToken cancellationToken);
    void Add(LearningProfile profile);
    Task SaveChangesAsync(CancellationToken cancellationToken);
}

public interface ILearningProfileReader
{
    Task<LearningProfileResult> GetAsync(Guid userId, CancellationToken cancellationToken);
}
