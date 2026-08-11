using DevRecall.Application.LearningProfiles;
using DevRecall.Domain.LearningProfiles;
using DevRecall.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace DevRecall.Infrastructure.LearningProfiles;

internal sealed class LearningProfileReader(DevRecallDbContext dbContext) : ILearningProfileReader
{
    public async Task<LearningProfileResult> GetAsync(Guid userId, CancellationToken cancellationToken)
    {
        var profile = await dbContext.LearningProfiles.AsNoTracking()
            .Where(item => item.UserId == userId)
            .Select(item => new
            {
                item.TargetRole, item.ExperienceLevel, item.AvailableMinutesPerDay,
                Technologies = item.Technologies.OrderBy(technology => technology.Technology)
                    .Select(technology => new { technology.Technology, technology.IsPrimary }).ToArray(),
                Goals = item.Goals.OrderBy(goal => goal.Goal).Select(goal => goal.Goal).ToArray(),
                item.Version, item.UpdatedAtUtc
            })
            .SingleOrDefaultAsync(cancellationToken);
        if (profile is null) return new(false, null, null, null, [], [], null, null);
        var configured = Enum.IsDefined(profile.TargetRole) && Enum.IsDefined(profile.ExperienceLevel)
            && profile.AvailableMinutesPerDay is >= LearningProfile.MinimumAvailableMinutes
                and <= LearningProfile.MaximumAvailableMinutes
            && profile.Technologies.Length > 0 && profile.Goals.Length > 0;
        return new(configured, LearningProfileMetadata.RoleValue(profile.TargetRole),
            LearningProfileMetadata.LevelValue(profile.ExperienceLevel), profile.AvailableMinutesPerDay,
            profile.Technologies.Select(item => LearningProfileMetadata.TechnologyValue(
                item.Technology, item.IsPrimary)).ToArray(),
            profile.Goals.Select(LearningProfileMetadata.GoalValue).ToArray(), profile.Version,
            profile.UpdatedAtUtc);
    }
}
