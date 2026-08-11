using DevRecall.Application.Common.Exceptions;
using DevRecall.Application.Common.Time;
using DevRecall.Application.Identity;
using DevRecall.Domain.LearningProfiles;

namespace DevRecall.Application.LearningProfiles;

public sealed class LearningProfileHandler(ILearningProfileRepository repository,
    ILearningProfileReader reader, ICurrentUser currentUser, IUtcClock utcClock)
{
    public Task<LearningProfileResult> GetAsync(CancellationToken cancellationToken) =>
        reader.GetAsync(GetUserId(), cancellationToken);

    public static LearningProfileOptionsResult GetOptions() => new(
        LearningProfileMetadata.RoleOptions(), LearningProfileMetadata.LevelOptions(),
        LearningProfileMetadata.TechnologyGroups(), LearningProfileMetadata.GoalOptions(),
        [15, 30, 45, 60, 90, 120]);

    public async Task<LearningProfileResult> PutAsync(PutLearningProfileCommand command,
        CancellationToken cancellationToken)
    {
        var role = Parse<TargetRole>(command.TargetRole, "targetRole");
        var level = Parse<ExperienceLevel>(command.ExperienceLevel, "experienceLevel");
        var technologies = ParseTechnologies(command.Technologies);
        var goals = ParseMany<LearningProfileGoal>(command.Goals, "goals");
        Validate(command, technologies, goals);

        var userId = GetUserId();
        var profile = await repository.GetAsync(userId, cancellationToken);
        if (profile is null)
        {
            if (command.ExpectedVersion is not null) throw Conflict();
            profile = LearningProfile.Create(Guid.NewGuid(), userId, role, level,
                command.AvailableMinutesPerDay, technologies, goals, utcClock.UtcNow);
            repository.Add(profile);
            await repository.SaveChangesAsync(cancellationToken);
        }
        else
        {
            if (command.ExpectedVersion != profile.Version) throw Conflict();
            if (profile.Update(role, level, command.AvailableMinutesPerDay,
                technologies, goals, utcClock.UtcNow))
                await repository.SaveChangesAsync(cancellationToken);
        }

        return Map(profile);
    }

    private Guid GetUserId() => currentUser.UserId ?? throw new UnauthorizedException(
        "IDENTITY_UNAUTHENTICATED", "Authentication is required.");

    private static T Parse<T>(string? value, string field) where T : struct, Enum
    {
        if (value is not null && Enum.TryParse<T>(value, false, out var parsed)
            && Enum.IsDefined(parsed) && !int.TryParse(value, out _)) return parsed;
        throw new ValidationException(new Dictionary<string, string[]>
        {
            [field] = [$"{field} is not supported."]
        });
    }

    private static (Technology Technology, bool IsPrimary)[] ParseTechnologies(
        IReadOnlyList<LearningProfileTechnologyInput>? values)
    {
        if (values is null) throw Required("technologies");
        return values.Select(item => (Parse<Technology>(item.Name, "technologies"), item.IsPrimary)).ToArray();
    }

    private static T[] ParseMany<T>(IReadOnlyList<string>? values, string field) where T : struct, Enum
    {
        if (values is null) throw Required(field);
        return values.Select(value => Parse<T>(value, field)).ToArray();
    }

    private static void Validate(PutLearningProfileCommand command,
        (Technology Technology, bool IsPrimary)[] technologies,
        LearningProfileGoal[] goals)
    {
        var errors = new Dictionary<string, string[]>();
        if (command.AvailableMinutesPerDay is < LearningProfile.MinimumAvailableMinutes or > LearningProfile.MaximumAvailableMinutes)
            errors["availableMinutesPerDay"] = ["Available study time must be between 5 and 480 minutes."];
        if (technologies.Length is < 1 or > LearningProfile.MaximumTechnologies)
            errors["technologies"] = ["Choose between one and twenty technologies."];
        else if (technologies.Select(item => item.Technology).Distinct().Count() != technologies.Length)
            errors["technologies"] = ["Technologies must be unique."];
        else if (technologies.Count(item => item.IsPrimary) > LearningProfile.MaximumPrimaryTechnologies)
            errors["technologies"] = ["Choose no more than five primary technologies."];
        if (goals.Length is < 1 or > LearningProfile.MaximumGoals)
            errors["goals"] = ["Choose between one and ten goals."];
        else if (goals.Distinct().Count() != goals.Length)
            errors["goals"] = ["Goals must be unique."];
        if (errors.Count > 0) throw new ValidationException(errors);
    }

    private static ValidationException Required(string field) => new(
        new Dictionary<string, string[]> { [field] = [$"{field} is required."] });
    private static ConcurrencyException Conflict() => new("LEARNING_PROFILE_CONFLICT",
        "The learning profile changed. Reload the latest version before saving again.");

    internal static LearningProfileResult Map(LearningProfile profile) => new(profile.IsConfigured,
        LearningProfileMetadata.RoleValue(profile.TargetRole),
        LearningProfileMetadata.LevelValue(profile.ExperienceLevel),
        profile.AvailableMinutesPerDay,
        profile.Technologies.OrderBy(item => item.Technology)
            .Select(item => LearningProfileMetadata.TechnologyValue(item.Technology, item.IsPrimary)).ToArray(),
        profile.Goals.Select(item => LearningProfileMetadata.GoalValue(item.Goal))
            .OrderBy(item => item.Value).ToArray(), profile.Version, profile.UpdatedAtUtc);
}
