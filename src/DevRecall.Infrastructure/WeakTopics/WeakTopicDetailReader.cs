using DevRecall.Application.WeakTopics.GetDetail;
using DevRecall.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace DevRecall.Infrastructure.WeakTopics;

internal sealed class WeakTopicDetailReader(DevRecallDbContext dbContext)
    : IWeakTopicDetailReader
{
    public Task<WeakTopicDetailReadModel?> FindAsync(
        Guid userId, Guid profileId, CancellationToken cancellationToken) =>
        dbContext.WeakTopicProfiles.AsNoTracking()
            .Where(x => x.Id == profileId && x.UserId == userId)
            .Select(x => new WeakTopicDetailReadModel(
                x.Id, x.ResourceType, x.ResourceId, x.Score, x.Level,
                x.SignalCount, x.Version, x.CalculatedAtUtc,
                x.CreatedAtUtc, x.UpdatedAtUtc))
            .SingleOrDefaultAsync(cancellationToken);
}
