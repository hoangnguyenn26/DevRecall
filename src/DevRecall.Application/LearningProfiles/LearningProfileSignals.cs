using DevRecall.Application.Common.Exceptions;
using DevRecall.Application.Identity;
using DevRecall.Domain.LearningProfiles;

namespace DevRecall.Application.LearningProfiles;

public sealed record LearningProfileSignals(TargetRole? TargetRole,
    ExperienceLevel? ExperienceLevel, IReadOnlySet<Technology> PrimaryTechnologies,
    IReadOnlySet<Technology> OtherTechnologies, IReadOnlySet<LearningProfileGoal> Goals,
    int? AvailableMinutesPerDay, bool IsConfigured);

public sealed class GetCurrentLearningProfileSignalsHandler(
    ILearningProfileRepository repository, ICurrentUser currentUser)
{
    public async Task<LearningProfileSignals> HandleAsync(CancellationToken cancellationToken)
    {
        var userId = currentUser.UserId ?? throw new UnauthorizedException(
            "IDENTITY_UNAUTHENTICATED", "Authentication is required.");
        var profile = await repository.GetAsync(userId, cancellationToken);
        return profile is null
            ? new(null, null, new HashSet<Technology>(), new HashSet<Technology>(),
                new HashSet<LearningProfileGoal>(), null, false)
            : new(profile.TargetRole, profile.ExperienceLevel,
                profile.Technologies.Where(item => item.IsPrimary)
                    .Select(item => item.Technology).ToHashSet(),
                profile.Technologies.Where(item => !item.IsPrimary)
                    .Select(item => item.Technology).ToHashSet(),
                profile.Goals.Select(item => item.Goal).ToHashSet(),
                profile.AvailableMinutesPerDay, profile.IsConfigured);
    }
}
