using DevRecall.Application.Common.Pagination;
using DevRecall.Application.StudyPlans.GetList;
using DevRecall.Domain.StudyPlans;
using DevRecall.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace DevRecall.Infrastructure.StudyPlans;

internal sealed class StudyPlanListReader(DevRecallDbContext dbContext)
    : IStudyPlanListReader
{
    public async Task<PagedReadResult<StudyPlanListReadModel>> ReadAsync(
        Guid userId, StudyPlanStatus? status, int skip, int take,
        CancellationToken cancellationToken)
    {
        var query = dbContext.StudyPlans.AsNoTracking()
            .Where(plan => plan.UserId == userId);
        if (status is not null)
        {
            query = query.Where(plan => plan.Status == status.Value);
        }

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(plan => plan.UpdatedAtUtc)
            .ThenByDescending(plan => plan.GeneratedAtUtc)
            .ThenBy(plan => plan.Id)
            .Skip(skip).Take(take)
            .Select(plan => new StudyPlanListReadModel(
                plan.Id, plan.Title, plan.Status, plan.Items.Count,
                plan.Items.Select(item =>
                    (int?)item.PlannedDurationMinutes).Sum() ?? 0,
                plan.GeneratedAtUtc, plan.ExpiresAtUtc, plan.ReadyAtUtc,
                plan.ConvertedAtUtc, plan.ConvertedStudySessionId,
                plan.CancelledAtUtc, plan.UpdatedAtUtc, plan.Version))
            .ToListAsync(cancellationToken);
        return new(items, totalCount);
    }
}
