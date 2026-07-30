using DevRecall.Application.WeakTopics;
using DevRecall.Domain.WeakTopics;
using DevRecall.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace DevRecall.Infrastructure.WeakTopics;

internal sealed class WeakTopicProfileRepository(
    DevRecallDbContext dbContext)
    : IWeakTopicProfileRepository
{
    public Task<WeakTopicProfile?> GetByUserAndResourceForUpdateAsync(
        Guid userId, WeakTopicResourceType resourceType, Guid resourceId,
        CancellationToken cancellationToken) =>
        dbContext.WeakTopicProfiles.SingleOrDefaultAsync(
            profile => profile.UserId == userId
                && profile.ResourceType == resourceType
                && profile.ResourceId == resourceId,
            cancellationToken);

    public void Add(WeakTopicProfile profile) =>
        dbContext.WeakTopicProfiles.Add(profile);

    public Task SaveChangesAsync(CancellationToken cancellationToken) =>
        dbContext.SaveChangesAsync(cancellationToken);
}
