namespace DevRecall.Domain.LearningProfiles;

public sealed class LearningProfile
{
    public const int MinimumAvailableMinutes = 5;
    public const int MaximumAvailableMinutes = 480;
    public const int MaximumTechnologies = 20;
    public const int MaximumPrimaryTechnologies = 5;
    public const int MaximumGoals = 10;

    private readonly List<LearningProfileTechnology> _technologies = [];
    private readonly List<LearningProfileGoalEntry> _goals = [];
    private LearningProfile() { }

    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public TargetRole TargetRole { get; private set; }
    public ExperienceLevel ExperienceLevel { get; private set; }
    public int AvailableMinutesPerDay { get; private set; }
    public int Version { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public DateTimeOffset UpdatedAtUtc { get; private set; }
    public IReadOnlyCollection<LearningProfileTechnology> Technologies => _technologies.AsReadOnly();
    public IReadOnlyCollection<LearningProfileGoalEntry> Goals => _goals.AsReadOnly();
    public bool IsConfigured => Enum.IsDefined(TargetRole) && Enum.IsDefined(ExperienceLevel)
        && AvailableMinutesPerDay is >= MinimumAvailableMinutes and <= MaximumAvailableMinutes
        && _technologies.Count >= 1 && _goals.Count >= 1;

    public static LearningProfile Create(Guid id, Guid userId, TargetRole targetRole,
        ExperienceLevel experienceLevel, int availableMinutesPerDay,
        IReadOnlyCollection<(Technology Technology, bool IsPrimary)> technologies,
        IReadOnlyCollection<LearningProfileGoal> goals, DateTimeOffset currentUtc)
    {
        if (id == Guid.Empty || userId == Guid.Empty) throw new ArgumentException("Ids cannot be empty.");
        Validate(targetRole, experienceLevel, availableMinutesPerDay, technologies, goals, currentUtc);
        var profile = new LearningProfile
        {
            Id = id, UserId = userId, TargetRole = targetRole,
            ExperienceLevel = experienceLevel, AvailableMinutesPerDay = availableMinutesPerDay,
            Version = 1, CreatedAtUtc = currentUtc, UpdatedAtUtc = currentUtc
        };
        profile.ReplaceChildren(technologies, goals);
        return profile;
    }

    public bool Update(TargetRole targetRole, ExperienceLevel experienceLevel,
        int availableMinutesPerDay,
        IReadOnlyCollection<(Technology Technology, bool IsPrimary)> technologies,
        IReadOnlyCollection<LearningProfileGoal> goals, DateTimeOffset currentUtc)
    {
        Validate(targetRole, experienceLevel, availableMinutesPerDay, technologies, goals, currentUtc);
        var normalizedTechnologies = technologies.OrderBy(item => item.Technology).ToArray();
        var normalizedGoals = goals.Order().ToArray();
        var unchanged = TargetRole == targetRole && ExperienceLevel == experienceLevel
            && AvailableMinutesPerDay == availableMinutesPerDay
            && _technologies.OrderBy(item => item.Technology)
                .Select(item => (item.Technology, item.IsPrimary)).SequenceEqual(normalizedTechnologies)
            && _goals.Select(item => item.Goal).Order().SequenceEqual(normalizedGoals);
        if (unchanged) return false;

        TargetRole = targetRole;
        ExperienceLevel = experienceLevel;
        AvailableMinutesPerDay = availableMinutesPerDay;
        UpdatedAtUtc = currentUtc;
        Version = checked(Version + 1);
        ReplaceChildren(normalizedTechnologies, normalizedGoals);
        return true;
    }

    private void ReplaceChildren(
        IReadOnlyCollection<(Technology Technology, bool IsPrimary)> technologies,
        IReadOnlyCollection<LearningProfileGoal> goals)
    {
        _technologies.Clear();
        _technologies.AddRange(technologies.OrderBy(item => item.Technology)
            .Select(item => new LearningProfileTechnology(Guid.NewGuid(), Id, item.Technology, item.IsPrimary)));
        _goals.Clear();
        _goals.AddRange(goals.Order().Select(goal => new LearningProfileGoalEntry(Guid.NewGuid(), Id, goal)));
    }

    private static void Validate(TargetRole targetRole, ExperienceLevel experienceLevel,
        int availableMinutesPerDay,
        IReadOnlyCollection<(Technology Technology, bool IsPrimary)> technologies,
        IReadOnlyCollection<LearningProfileGoal> goals, DateTimeOffset currentUtc)
    {
        ArgumentNullException.ThrowIfNull(technologies);
        ArgumentNullException.ThrowIfNull(goals);
        if (!Enum.IsDefined(targetRole)) throw new ArgumentOutOfRangeException(nameof(targetRole));
        if (!Enum.IsDefined(experienceLevel)) throw new ArgumentOutOfRangeException(nameof(experienceLevel));
        if (availableMinutesPerDay is < MinimumAvailableMinutes or > MaximumAvailableMinutes)
            throw new ArgumentOutOfRangeException(nameof(availableMinutesPerDay));
        if (technologies.Count is < 1 or > MaximumTechnologies)
            throw new ArgumentException("Choose between one and twenty technologies.", nameof(technologies));
        if (goals.Count is < 1 or > MaximumGoals)
            throw new ArgumentException("Choose between one and ten goals.", nameof(goals));
        if (technologies.Select(item => item.Technology).Distinct().Count() != technologies.Count)
            throw new ArgumentException("Technologies must be unique.", nameof(technologies));
        if (technologies.Count(item => item.IsPrimary) > MaximumPrimaryTechnologies)
            throw new ArgumentException("At most five primary technologies are allowed.", nameof(technologies));
        if (goals.Distinct().Count() != goals.Count)
            throw new ArgumentException("Goals must be unique.", nameof(goals));
        if (technologies.Any(item => !Enum.IsDefined(item.Technology)))
            throw new ArgumentOutOfRangeException(nameof(technologies));
        if (goals.Any(goal => !Enum.IsDefined(goal)))
            throw new ArgumentOutOfRangeException(nameof(goals));
        if (currentUtc.Offset != TimeSpan.Zero) throw new ArgumentException("Timestamp must be UTC.", nameof(currentUtc));
    }
}
