using DevRecall.Application.WeakTopics;
using DevRecall.Domain.WeakTopics;
using DevRecall.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Npgsql;

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

    public async Task<IReadOnlyList<WeakTopicProfile>> GetByUserIdForUpdateAsync(
        Guid userId, CancellationToken cancellationToken) =>
        await dbContext.WeakTopicProfiles.Where(x => x.UserId == userId)
            .ToListAsync(cancellationToken);

    public async Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new WeakTopicProfileConflictException();
        }
        catch (DbUpdateException exception)
            when (exception.InnerException is PostgresException
            {
                ConstraintName: "ux_weak_topic_profiles_user_resource"
            })
        {
            throw new WeakTopicProfileConflictException();
        }
    }
}
