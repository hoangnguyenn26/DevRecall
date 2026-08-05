namespace DevRecall.Contracts.Today;

public sealed record GetTodayDashboardResponse(
    DateTimeOffset GeneratedAtUtc,
    TodayUserSummaryResponse User,
    TodayNextActionResponse NextAction,
    TodayRecentActivityResponse? RecentActivity,
    TodayMetricsResponse Metrics,
    TodayStudyPlanResponse? StudyPlan,
    IReadOnlyList<TodayRecommendationResponse> Recommendations,
    IReadOnlyList<TodayWeakTopicResponse> WeakTopics,
    IReadOnlyList<TodayActivityPointResponse> WeeklyActivity);
public sealed record TodayUserSummaryResponse(
    Guid UserId, string DisplayName, bool HasCompletedOnboarding);
public sealed record TodayNextActionResponse(
    string Type, string Title, string Description, string ActionLabel,
    string TargetPath, string Icon, TodayNextActionContextResponse? Context);
public sealed record TodayNextActionContextResponse(
    Guid? ResourceId, string? ResourceType, string? ResourceTitle,
    int? PlannedDurationMinutes, int? RemainingCount, string? Priority);
public sealed record TodayRecentActivityResponse(
    string Type, Guid ResourceId, string Title, string Description,
    DateTimeOffset OccurredAtUtc, string TargetPath, string Icon);
public sealed record TodayMetricsResponse(
    int ReviewsDue, int StudyMinutesThisWeek, int ActiveDaysThisWeek,
    int WeeklyTargetDays, decimal WeeklyProgressPercent);
public sealed record TodayStudyPlanResponse(
    Guid StudyPlanId, string Title, string Status, int ItemCount,
    int TotalPlannedDurationMinutes, int Version,
    IReadOnlyList<TodayStudyPlanItemResponse> Items);
public sealed record TodayStudyPlanItemResponse(
    Guid ItemId, string ResourceType, Guid ResourceId, string ResourceTitle,
    bool IsResourceAvailable, int PlannedDurationMinutes, int Position);
public sealed record TodayRecommendationResponse(
    Guid RecommendationId, string ResourceType, Guid ResourceId,
    string Type, string Priority, decimal PriorityScore, string ResourceTitle,
    string ReasonSummary, bool IsResourceAvailable);
public sealed record TodayWeakTopicResponse(
    Guid WeakTopicProfileId, string ResourceType, Guid ResourceId,
    string Level, decimal Score, string ResourceTitle, string Summary,
    bool IsResourceAvailable);
public sealed record TodayActivityPointResponse(
    DateOnly Date, int StudyMinutes, int ActivityCount);
