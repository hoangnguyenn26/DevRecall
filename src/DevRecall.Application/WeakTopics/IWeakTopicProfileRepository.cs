using DevRecall.Domain.WeakTopics;

namespace DevRecall.Application.WeakTopics;

public interface IWeakTopicProfileRepository
{
    Task<WeakTopicProfile?> GetByUserAndResourceForUpdateAsync(
        Guid userId, WeakTopicResourceType resourceType, Guid resourceId,
        CancellationToken cancellationToken);
    Task<IReadOnlyList<WeakTopicProfile>> GetByUserIdForUpdateAsync(
        Guid userId, CancellationToken cancellationToken);

    void Add(WeakTopicProfile profile);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}

public sealed class WeakTopicProfileConflictException : Exception;
