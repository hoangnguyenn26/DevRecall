using DevRecall.Application.Common.Pagination;
using DevRecall.Application.WeakTopics.GetList;
using DevRecall.Domain.WeakTopics;
using DevRecall.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace DevRecall.Infrastructure.WeakTopics;

internal sealed class WeakTopicListReader(DevRecallDbContext dbContext)
    : IWeakTopicListReader
{
    public async Task<PagedReadResult<WeakTopicListReadModel>> ReadAsync(
        Guid userId, WeaknessLevel? level, WeakTopicResourceType? resourceType,
        decimal? minimumScore, bool includeNone, int skip, int take,
        CancellationToken cancellationToken)
    {
        var query = dbContext.WeakTopicProfiles.AsNoTracking()
            .Where(x => x.UserId == userId);
        if (level is not null)
        {
            query = query.Where(x => x.Level == level.Value);
        }
        else if (!includeNone)
        {
            query = query.Where(x => x.Level != WeaknessLevel.None);
        }

        if (resourceType is not null)
        {
            query = query.Where(x => x.ResourceType == resourceType.Value);
        }

        if (minimumScore is not null)
        {
            query = query.Where(x => x.Score >= minimumScore.Value);
        }

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query.OrderByDescending(x => x.Level)
            .ThenByDescending(x => x.Score)
            .ThenByDescending(x => x.CalculatedAtUtc).ThenBy(x => x.Id)
            .Skip(skip).Take(take)
            .Select(x => new WeakTopicListReadModel(
                x.Id, x.ResourceType, x.ResourceId, x.Score, x.Level,
                x.SignalCount, x.Version, x.CalculatedAtUtc, x.UpdatedAtUtc))
            .ToListAsync(cancellationToken);
        return new(items, totalCount);
    }
}
