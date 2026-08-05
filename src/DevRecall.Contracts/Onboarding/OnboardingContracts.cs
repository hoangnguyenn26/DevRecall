namespace DevRecall.Contracts.Onboarding;

public sealed record GetOnboardingResponse(
    bool HasCompleted, string? CompletionType, string? Goal,
    int? DailyCommitmentMinutes, int? WeeklyTargetDays,
    IReadOnlyList<string> FocusAreas);
public sealed record CompleteOnboardingRequest(
    string Goal, int DailyCommitmentMinutes, int WeeklyTargetDays,
    IReadOnlyList<string> FocusAreas);
