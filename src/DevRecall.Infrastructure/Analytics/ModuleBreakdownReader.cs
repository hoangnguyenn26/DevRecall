using DevRecall.Application.Analytics;
using DevRecall.Application.Analytics.ModuleBreakdown;
using DevRecall.Domain.Study;
using DevRecall.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace DevRecall.Infrastructure.Analytics;

internal sealed class ModuleBreakdownReader(DevRecallDbContext dbContext)
    : IModuleBreakdownReader
{
    public async Task<IReadOnlyList<ModuleActivityCount>> ReadAsync(
        Guid userId, AnalyticsDateRange range,
        CancellationToken cancellationToken)
    {
        var query =
            from item in dbContext.StudySessionItems.AsNoTracking()
            join session in dbContext.StudySessions.AsNoTracking()
                on item.StudySessionId equals session.Id
            where session.UserId == userId
                && item.Status == StudySessionItemStatus.Completed
                && item.CompletedAtUtc >= range.FromUtc
                && item.CompletedAtUtc < range.ToUtc
            group item by item.ResourceType
            into grouped
            select new ModuleActivityCount(grouped.Key, grouped.Count());
        return await query.ToListAsync(cancellationToken);
    }
}
