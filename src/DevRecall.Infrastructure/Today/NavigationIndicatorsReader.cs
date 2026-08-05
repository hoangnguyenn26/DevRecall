using DevRecall.Application.Navigation;
using DevRecall.Domain.Reviews;
using DevRecall.Domain.StudyPlans;
using DevRecall.Domain.WeakTopics;
using DevRecall.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace DevRecall.Infrastructure.Today;

internal sealed class NavigationIndicatorsReader(DevRecallDbContext dbContext) : INavigationIndicatorsReader
{
    public async Task<NavigationIndicatorsReadModel> ReadAsync(
        Guid userId, DateTimeOffset currentUtc, CancellationToken cancellationToken)
    {
        var reviewsDue = await dbContext.ReviewItems.AsNoTracking().CountAsync(
            x => x.UserId == userId && x.Status == ReviewItemStatus.Active && x.DueAtUtc <= currentUtc,
            cancellationToken);
        var hasActivePlan = await dbContext.StudyPlans.AsNoTracking().AnyAsync(
            x => x.UserId == userId && (x.Status == StudyPlanStatus.Draft || x.Status == StudyPlanStatus.Ready),
            cancellationToken);
        var criticalWeakTopics = await dbContext.WeakTopicProfiles.AsNoTracking().CountAsync(
            x => x.UserId == userId && x.Level == WeaknessLevel.Critical, cancellationToken);
        var hasPreferences = await dbContext.UserLearningPreferences.AsNoTracking()
            .AnyAsync(x => x.UserId == userId, cancellationToken);
        return new(reviewsDue, hasActivePlan, criticalWeakTopics, !hasPreferences);
    }
}
