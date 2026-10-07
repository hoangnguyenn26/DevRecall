using DevRecall.Application.StudyPlans.LearningContent;
using DevRecall.Domain.LearningContent;
using DevRecall.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace DevRecall.Infrastructure.StudyPlans;

internal sealed class LearningContentPlanSourceReader(DevRecallDbContext dbContext)
    : ILearningContentPlanSourceReader
{
    public async Task<LearningContentPlanSource?> FindPublishedAsync(Guid userId, string slug,
        CancellationToken cancellationToken) => await dbContext.LearningContents.AsNoTracking()
        .Where(item => item.Slug == slug && item.Status == ContentStatus.Published)
        .Select(item => new LearningContentPlanSource(item.Id, item.Title, item.EstimatedMinutes,
            item.ContentType == LearningContentType.Lesson && dbContext.LearningContentProgresses.Any(progress => progress.UserId == userId
                && progress.LearningContentId == item.Id
                && progress.Status == LearningProgressStatus.Completed)))
        .SingleOrDefaultAsync(cancellationToken);

    public async Task<IReadOnlyList<LearningContentStudyPlanOption>> GetDraftOptionsAsync(Guid userId,
        string slug, CancellationToken cancellationToken)
    {
        var lessonId = await dbContext.LearningContents.AsNoTracking()
            .Where(item => item.Slug == slug && item.Status == ContentStatus.Published)
            .Select(item => (Guid?)item.Id).SingleOrDefaultAsync(cancellationToken);
        if (lessonId is null) return [];
        return await dbContext.StudyPlans.AsNoTracking()
            .Where(plan => plan.UserId == userId && plan.Status == Domain.StudyPlans.StudyPlanStatus.Draft)
            .OrderByDescending(plan => plan.Title)
            .Select(plan => new LearningContentStudyPlanOption(plan.Id, plan.Title, plan.Items.Count,
                plan.Items.Sum(item => item.PlannedDurationMinutes), plan.Version,
                plan.Items.Any(item => item.ResourceType == Domain.StudyPlans.StudyPlanResourceType.LearningContent
                    && item.ResourceId == lessonId.Value)))
            .ToListAsync(cancellationToken);
    }
}
