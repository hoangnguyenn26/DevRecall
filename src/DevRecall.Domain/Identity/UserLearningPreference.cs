namespace DevRecall.Domain.Identity;

public sealed class UserLearningPreference
{
    private readonly List<UserLearningFocusArea> _focusAreas = [];

    private UserLearningPreference()
    {
    }

    public Guid UserId { get; private set; }
    public LearningGoal Goal { get; private set; }
    public int DailyCommitmentMinutes { get; private set; }
    public int WeeklyTargetDays { get; private set; }
    public OnboardingCompletionType CompletionType { get; private set; }
    public DateTimeOffset OnboardingCompletedAtUtc { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public DateTimeOffset UpdatedAtUtc { get; private set; }
    public int Version { get; private set; }
    public IReadOnlyCollection<UserLearningFocusArea> FocusAreas =>
        _focusAreas.AsReadOnly();

    public static UserLearningPreference Complete(
        Guid userId, LearningGoal goal, int dailyCommitmentMinutes,
        int weeklyTargetDays, IReadOnlyCollection<LearningFocusArea> focusAreas,
        DateTimeOffset currentUtc)
    {
        var preference = Create(userId, currentUtc);
        preference.Apply(goal, dailyCommitmentMinutes, weeklyTargetDays,
            focusAreas, OnboardingCompletionType.Completed, currentUtc);
        return preference;
    }

    public static UserLearningPreference Skip(Guid userId, DateTimeOffset currentUtc) =>
        Complete(userId, LearningPreferenceDefaults.Goal,
            LearningPreferenceDefaults.DailyCommitmentMinutes,
            LearningPreferenceDefaults.WeeklyTargetDays, [], currentUtc)
            .WithCompletionType(OnboardingCompletionType.Skipped);

    public bool CompleteOnboarding(
        LearningGoal goal, int dailyCommitmentMinutes, int weeklyTargetDays,
        IReadOnlyCollection<LearningFocusArea> focusAreas,
        DateTimeOffset currentUtc)
    {
        Validate(goal, dailyCommitmentMinutes, weeklyTargetDays, focusAreas,
            currentUtc);
        var normalizedAreas = focusAreas.Distinct().Order().ToArray();
        if (CompletionType == OnboardingCompletionType.Completed
            && Goal == goal
            && DailyCommitmentMinutes == dailyCommitmentMinutes
            && WeeklyTargetDays == weeklyTargetDays
            && _focusAreas.Select(item => item.Area).Order()
                .SequenceEqual(normalizedAreas))
        {
            return false;
        }

        Apply(goal, dailyCommitmentMinutes, weeklyTargetDays, normalizedAreas,
            OnboardingCompletionType.Completed, currentUtc);
        Version = checked(Version + 1);
        return true;
    }

    private static UserLearningPreference Create(
        Guid userId, DateTimeOffset currentUtc)
    {
        if (userId == Guid.Empty)
        {
            throw new ArgumentException("User id cannot be empty.", nameof(userId));
        }

        EnsureUtc(currentUtc);
        return new UserLearningPreference
        {
            UserId = userId,
            CreatedAtUtc = currentUtc,
            UpdatedAtUtc = currentUtc,
            OnboardingCompletedAtUtc = currentUtc,
            Version = 1
        };
    }

    private void Apply(
        LearningGoal goal, int dailyCommitmentMinutes, int weeklyTargetDays,
        IReadOnlyCollection<LearningFocusArea> focusAreas,
        OnboardingCompletionType completionType, DateTimeOffset currentUtc)
    {
        Validate(goal, dailyCommitmentMinutes, weeklyTargetDays, focusAreas,
            currentUtc);
        var normalizedAreas = focusAreas.Distinct().Order().ToArray();
        Goal = goal;
        DailyCommitmentMinutes = dailyCommitmentMinutes;
        WeeklyTargetDays = weeklyTargetDays;
        CompletionType = completionType;
        OnboardingCompletedAtUtc = currentUtc;
        UpdatedAtUtc = currentUtc;
        SynchronizeFocusAreas(normalizedAreas);
    }

    private UserLearningPreference WithCompletionType(
        OnboardingCompletionType completionType)
    {
        CompletionType = completionType;
        return this;
    }

    private void SynchronizeFocusAreas(
        IReadOnlyCollection<LearningFocusArea> areas)
    {
        _focusAreas.RemoveAll(item => !areas.Contains(item.Area));
        foreach (var area in areas.Where(area =>
                     _focusAreas.All(item => item.Area != area)))
        {
            _focusAreas.Add(new UserLearningFocusArea(UserId, area));
        }
    }

    private static void Validate(
        LearningGoal goal, int dailyCommitmentMinutes, int weeklyTargetDays,
        IReadOnlyCollection<LearningFocusArea> focusAreas,
        DateTimeOffset currentUtc)
    {
        ArgumentNullException.ThrowIfNull(focusAreas);
        if (!Enum.IsDefined(goal))
        {
            throw new ArgumentOutOfRangeException(nameof(goal));
        }

        if (dailyCommitmentMinutes is < LearningPreferenceDefaults.MinimumDailyCommitmentMinutes
            or > LearningPreferenceDefaults.MaximumDailyCommitmentMinutes)
        {
            throw new ArgumentOutOfRangeException(nameof(dailyCommitmentMinutes));
        }

        if (weeklyTargetDays is < LearningPreferenceDefaults.MinimumWeeklyTargetDays
            or > LearningPreferenceDefaults.MaximumWeeklyTargetDays)
        {
            throw new ArgumentOutOfRangeException(nameof(weeklyTargetDays));
        }

        var distinctAreas = focusAreas.Distinct().ToArray();
        if (distinctAreas.Length > LearningPreferenceDefaults.MaximumFocusAreas)
        {
            throw new ArgumentException("At most four focus areas are allowed.",
                nameof(focusAreas));
        }

        if (distinctAreas.Any(area => !Enum.IsDefined(area)))
        {
            throw new ArgumentOutOfRangeException(nameof(focusAreas));
        }

        EnsureUtc(currentUtc);
    }

    private static void EnsureUtc(DateTimeOffset currentUtc)
    {
        if (currentUtc.Offset != TimeSpan.Zero)
        {
            throw new ArgumentException("Timestamp must be UTC.", nameof(currentUtc));
        }
    }
}
