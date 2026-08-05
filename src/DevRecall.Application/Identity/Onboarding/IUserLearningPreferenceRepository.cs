using DevRecall.Domain.Identity;

namespace DevRecall.Application.Identity.Onboarding;

public interface IUserLearningPreferenceRepository
{
    Task<UserLearningPreference?> GetAsync(
        Guid userId, CancellationToken cancellationToken);
    void Add(UserLearningPreference preference);
    Task SaveChangesAsync(CancellationToken cancellationToken);
}
