using DevRecall.Application.LearningProfiles;
using DevRecall.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace DevRecall.Infrastructure.LearningProfiles;

internal sealed class LearningProfileReader(DevRecallDbContext dbContext) : ILearningProfileReader
{
    public async Task<LearningProfileResult> GetAsync(Guid userId, CancellationToken cancellationToken)
    {
        var profile = await dbContext.LearningProfiles.AsNoTracking()
            .Where(item => item.UserId == userId)
            .Select(item => new LearningProfileResult(true, item.TargetRole.ToString(),
                item.ExperienceLevel.ToString(), item.AvailableMinutesPerDay,
                item.Technologies.OrderBy(technology => technology.Technology)
                    .Select(technology => new LearningProfileTechnologyResult(
                        technology.Technology.ToString(), technology.IsPrimary)).ToArray(),
                item.Goals.OrderBy(goal => goal.Goal).Select(goal => goal.Goal.ToString()).ToArray(),
                item.Version))
            .SingleOrDefaultAsync(cancellationToken);
        return profile ?? new LearningProfileResult(false, null, null, null, [], [], null);
    }
}
