namespace DevRecall.Contracts.Navigation;

public sealed record NavigationIndicatorsResponse(
    int ReviewsDue, bool HasActiveStudyPlan, int CriticalWeakTopics, bool HasIncompleteOnboarding);
