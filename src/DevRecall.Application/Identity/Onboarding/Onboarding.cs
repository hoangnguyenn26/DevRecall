using DevRecall.Application.Common.Exceptions;
using DevRecall.Application.Common.Time;
using DevRecall.Application.Identity;
using DevRecall.Domain.Identity;

namespace DevRecall.Application.Identity.Onboarding;

public sealed record GetOnboardingResult(
    bool HasCompleted, string? CompletionType, string? Goal,
    int? DailyCommitmentMinutes, int? WeeklyTargetDays,
    IReadOnlyList<string> FocusAreas);
public sealed record CompleteOnboardingCommand(
    string Goal, int DailyCommitmentMinutes, int WeeklyTargetDays,
    IReadOnlyList<string> FocusAreas);

public sealed class OnboardingHandler(
    IUserLearningPreferenceRepository repository, ICurrentUser currentUser,
    IUtcClock utcClock)
{
    public async Task<GetOnboardingResult> GetAsync(
        CancellationToken cancellationToken)
    {
        var preference = await repository.GetAsync(
            GetUserId(), cancellationToken);
        return preference is null
            ? new(false, null, null, null, null, [])
            : Map(preference);
    }

    public async Task<GetOnboardingResult> CompleteAsync(
        CompleteOnboardingCommand command, CancellationToken cancellationToken)
    {
        var goal = ParseGoal(command.Goal);
        var focusAreas = ParseFocusAreas(command.FocusAreas);
        Validate(command, focusAreas);
        var userId = GetUserId();
        var preference = await repository.GetAsync(userId, cancellationToken);
        if (preference is null)
        {
            preference = UserLearningPreference.Complete(
                userId, goal, command.DailyCommitmentMinutes,
                command.WeeklyTargetDays, focusAreas, utcClock.UtcNow);
            repository.Add(preference);
        }
        else
        {
            preference.CompleteOnboarding(
                goal, command.DailyCommitmentMinutes,
                command.WeeklyTargetDays, focusAreas, utcClock.UtcNow);
        }

        await repository.SaveChangesAsync(cancellationToken);
        return Map(preference);
    }

    public async Task<GetOnboardingResult> SkipAsync(
        CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        var preference = await repository.GetAsync(userId, cancellationToken);
        if (preference?.CompletionType == OnboardingCompletionType.Completed)
        {
            throw new ConflictException(
                "ONBOARDING_ALREADY_COMPLETED",
                "Completed onboarding preferences cannot be overwritten by skip.");
        }

        if (preference is null)
        {
            preference = UserLearningPreference.Skip(userId, utcClock.UtcNow);
            repository.Add(preference);
            await repository.SaveChangesAsync(cancellationToken);
        }

        return Map(preference);
    }

    private Guid GetUserId() => currentUser.UserId
        ?? throw new UnauthorizedException(
            "IDENTITY_UNAUTHENTICATED", "Authentication is required.");

    private static LearningGoal ParseGoal(string value)
    {
        var values = new Dictionary<string, LearningGoal>(StringComparer.Ordinal)
        {
            [nameof(LearningGoal.PrepareForInterviews)] = LearningGoal.PrepareForInterviews,
            [nameof(LearningGoal.StrengthenDotNetSkills)] = LearningGoal.StrengthenDotNetSkills,
            [nameof(LearningGoal.PracticeAlgorithms)] = LearningGoal.PracticeAlgorithms,
            [nameof(LearningGoal.BuildConsistentStudyHabit)] = LearningGoal.BuildConsistentStudyHabit
        };
        if (!values.TryGetValue(value ?? string.Empty, out var result))
        {
            throw new ValidationException(new Dictionary<string, string[]>
            {
                ["goal"] = ["Goal is not supported."]
            });
        }

        return result;
    }

    private static LearningFocusArea[] ParseFocusAreas(
        IReadOnlyList<string>? values)
    {
        if (values is null)
        {
            throw new ValidationException(new Dictionary<string, string[]>
            {
                ["focusAreas"] = ["Focus areas are required."]
            });
        }

        var allowed = new Dictionary<string, LearningFocusArea>(StringComparer.Ordinal)
        {
            [nameof(LearningFocusArea.CSharp)] = LearningFocusArea.CSharp,
            [nameof(LearningFocusArea.DotNet)] = LearningFocusArea.DotNet,
            [nameof(LearningFocusArea.SystemDesign)] = LearningFocusArea.SystemDesign,
            [nameof(LearningFocusArea.Databases)] = LearningFocusArea.Databases,
            [nameof(LearningFocusArea.AlgorithmsAndDataStructures)] = LearningFocusArea.AlgorithmsAndDataStructures,
            [nameof(LearningFocusArea.InterviewCommunication)] = LearningFocusArea.InterviewCommunication
        };
        var invalid = values.Where(value => !allowed.ContainsKey(value)).ToArray();
        if (invalid.Length > 0)
        {
            throw new ValidationException(new Dictionary<string, string[]>
            {
                ["focusAreas"] = ["One or more focus areas are not supported."]
            });
        }

        return values.Select(value => allowed[value]).Distinct().ToArray();
    }

    private static void Validate(
        CompleteOnboardingCommand command,
        LearningFocusArea[] focusAreas)
    {
        var errors = new Dictionary<string, string[]>();
        if (command.DailyCommitmentMinutes is
            < LearningPreferenceDefaults.MinimumDailyCommitmentMinutes
            or > LearningPreferenceDefaults.MaximumDailyCommitmentMinutes)
        {
            errors["dailyCommitmentMinutes"] =
                ["Daily commitment must be between 10 and 180 minutes."];
        }

        if (command.WeeklyTargetDays is
            < LearningPreferenceDefaults.MinimumWeeklyTargetDays
            or > LearningPreferenceDefaults.MaximumWeeklyTargetDays)
        {
            errors["weeklyTargetDays"] =
                ["Weekly target must be between 1 and 7 days."];
        }

        if (focusAreas.Length is < 1 or > LearningPreferenceDefaults.MaximumFocusAreas)
        {
            errors["focusAreas"] = ["Choose between one and four focus areas."];
        }

        if (errors.Count > 0)
        {
            throw new ValidationException(errors);
        }
    }

    private static GetOnboardingResult Map(UserLearningPreference preference) =>
        new(true, preference.CompletionType.ToString(), preference.Goal.ToString(),
            preference.DailyCommitmentMinutes, preference.WeeklyTargetDays,
            preference.FocusAreas.Select(item => item.Area.ToString())
                .Order().ToArray());
}
