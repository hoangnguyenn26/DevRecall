using DevRecall.Application.StudyPlans.GetDetail;
using DevRecall.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace DevRecall.Infrastructure.StudyPlans;

internal sealed class StudyPlanDetailReader(DevRecallDbContext dbContext)
    : IStudyPlanDetailReader
{
    public async Task<StudyPlanDetailReadModel?> FindAsync(
        Guid userId, Guid studyPlanId, CancellationToken cancellationToken)
    {
        var plan = await dbContext.StudyPlans.AsNoTracking()
            .Where(item => item.Id == studyPlanId && item.UserId == userId)
            .Select(item => new
            {
                item.Id,
                item.Title,
                item.Status,
                item.GeneratedAtUtc,
                item.ExpiresAtUtc,
                item.ReadyAtUtc,
                item.ConvertedAtUtc,
                item.ConvertedStudySessionId,
                item.CancelledAtUtc,
                item.CreatedAtUtc,
                item.UpdatedAtUtc,
                item.Version
            }).SingleOrDefaultAsync(cancellationToken);
        if (plan is null)
        {
            return null;
        }

        var items = await dbContext.StudyPlanItems.AsNoTracking()
            .Where(item => item.StudyPlanId == studyPlanId)
            .OrderBy(item => item.Position).ThenBy(item => item.Id)
            .Select(item => new StudyPlanItemReadModel(
                item.Id, item.SourceRecommendationId, item.SourceType,
                item.ResourceType, item.ResourceId,
                item.PlannedDurationMinutes, item.Position))
            .ToListAsync(cancellationToken);
        return new(
            plan.Id, plan.Title, plan.Status, plan.GeneratedAtUtc,
            plan.ExpiresAtUtc, plan.ReadyAtUtc, plan.ConvertedAtUtc,
            plan.ConvertedStudySessionId, plan.CancelledAtUtc,
            plan.CreatedAtUtc, plan.UpdatedAtUtc, plan.Version, items);
    }
}
