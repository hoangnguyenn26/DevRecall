using DevRecall.Application.Identity.Onboarding;
using DevRecall.Domain.Identity;
using DevRecall.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace DevRecall.Infrastructure.Identity;

internal sealed class UserLearningPreferenceRepository(
    DevRecallDbContext dbContext) : IUserLearningPreferenceRepository
{
    public Task<UserLearningPreference?> GetAsync(
        Guid userId, CancellationToken cancellationToken) =>
        dbContext.UserLearningPreferences
            .Include(item => item.FocusAreas)
            .SingleOrDefaultAsync(item => item.UserId == userId,
                cancellationToken);

    public void Add(UserLearningPreference preference) =>
        dbContext.UserLearningPreferences.Add(preference);

    public Task SaveChangesAsync(CancellationToken cancellationToken) =>
        dbContext.SaveChangesAsync(cancellationToken);
}
