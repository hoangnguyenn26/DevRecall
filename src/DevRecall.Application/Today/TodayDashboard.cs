using DevRecall.Application.Common.Exceptions;
using DevRecall.Application.Common.Time;
using DevRecall.Application.Identity;
using DevRecall.Application.LearningContent;
using DevRecall.Application.Discover;
using DevRecall.Domain.Identity;
using DevRecall.Domain.Recommendations;
using DevRecall.Domain.StudyPlans;
using DevRecall.Domain.WeakTopics;

namespace DevRecall.Application.Today;

public static class TodayDashboardDefaults
{
    public const int WeeklyTargetDays = LearningPreferenceDefaults.WeeklyTargetDays;
}

public sealed record GetTodayDashboardQuery;

public sealed record TodayDashboardMetricsReadModel(
    int ReviewsDue, int StudyMinutesThisWeek, int ActiveDaysThisWeek,
    int WeeklyTargetDays, decimal WeeklyProgressPercent);
public sealed record TodayStudyPlanItemReadModel(
    Guid ItemId, string ResourceType, Guid ResourceId, string ResourceTitle,
    bool IsResourceAvailable, int PlannedDurationMinutes, int Position);
public sealed record TodayStudyPlanReadModel(
    Guid StudyPlanId, string Title, StudyPlanStatus Status, int ItemCount,
    int TotalPlannedDurationMinutes, int Version,
    IReadOnlyList<TodayStudyPlanItemReadModel> Items);
public sealed record TodayRecommendationReadModel(
    Guid RecommendationId, RecommendationResourceType ResourceType,
    Guid ResourceId, RecommendationType Type, RecommendationPriority Priority,
    decimal PriorityScore, string ResourceTitle, string ReasonSummary,
    bool IsResourceAvailable);
public sealed record TodayWeakTopicReadModel(
    Guid WeakTopicProfileId, WeakTopicResourceType ResourceType,
    Guid ResourceId, WeaknessLevel Level, decimal Score,
    string ResourceTitle, string Summary, bool IsResourceAvailable);
public sealed record TodayActivityPointReadModel(
    DateOnly Date, int StudyMinutes, int ActivityCount);
public enum TodayRecentActivityType { Knowledge = 1, InterviewPractice = 2, DsaAttempt = 3, StudyPlan = 4, StudySession = 5 }
public sealed record TodayRecentActivityReadModel(
    TodayRecentActivityType Type, Guid ResourceId, string Title, string Description,
    DateTimeOffset OccurredAtUtc, string TargetPath, string Icon);
public sealed record ActiveStudySessionCandidate(
    Guid Id, string Title, int RemainingItemCount, int? RemainingMinutes);
public sealed record TodayDashboardReadModel(
    string DisplayName, bool HasCompletedOnboarding,
    TodayDashboardMetricsReadModel Metrics,
    TodayStudyPlanReadModel? StudyPlan,
    IReadOnlyList<TodayRecommendationReadModel> Recommendations,
    IReadOnlyList<TodayWeakTopicReadModel> WeakTopics,
    IReadOnlyList<TodayActivityPointReadModel> WeeklyActivity,
    ActiveStudySessionCandidate? ActiveSession,
    int ActiveRecommendationCount)
{
    public bool HasActionablePlan { get; init; }
}

public interface ITodayDashboardReader
{
    Task<TodayDashboardReadModel> ReadAsync(
        Guid userId, DateTimeOffset currentUtc,
        CancellationToken cancellationToken);
}
public interface ITodayRecentActivityReader
{
    Task<TodayRecentActivityReadModel?> ReadLatestAsync(
        Guid userId, DateTimeOffset currentUtc, CancellationToken cancellationToken);
}

public sealed record TodayDashboardResult(
    DateTimeOffset GeneratedAtUtc, Guid UserId, string DisplayName,
    bool HasCompletedOnboarding,
    TodayNextAction NextAction, TodayRecentActivityReadModel? RecentActivity,
    TodayDashboardMetricsReadModel Metrics,
    TodayStudyPlanReadModel? StudyPlan,
    IReadOnlyList<TodayRecommendationReadModel> Recommendations,
    IReadOnlyList<TodayWeakTopicReadModel> WeakTopics,
    IReadOnlyList<TodayActivityPointReadModel> WeeklyActivity);

public sealed class GetTodayDashboardHandler(
    ITodayDashboardReader reader, ITodayNextActionPolicy nextActionPolicy,
    ITodayRecentActivityReader recentActivityReader,
    ICurrentUser currentUser, IUtcClock utcClock,
    ILearningContentReader learningContentReader, IDiscoverReader discoverReader)
{
    public async Task<TodayDashboardResult> HandleAsync(
        GetTodayDashboardQuery query, CancellationToken cancellationToken)
    {
        var userId = currentUser.UserId
            ?? throw new UnauthorizedException(
                "IDENTITY_UNAUTHENTICATED", "Authentication is required.");
        var currentUtc = utcClock.UtcNow;
        var dashboard = await reader.ReadAsync(
            userId, currentUtc, cancellationToken);
        var recentActivity = await recentActivityReader.ReadLatestAsync(
            userId, currentUtc, cancellationToken);
        ContinueLearningContentItem? continuing = null;
        if (dashboard.ActiveSession is null && dashboard.Metrics.ReviewsDue == 0)
        {
            var lessons = await learningContentReader.GetInProgressAsync(userId, 1, cancellationToken);
            continuing = lessons.Count > 0 ? lessons[0] : null;
        }
        DiscoverLesson? recommended = null;
        if (dashboard.ActiveSession is null && dashboard.Metrics.ReviewsDue == 0 && continuing is null
            && !dashboard.HasActionablePlan && dashboard.ActiveRecommendationCount == 0)
        {
            var inputs = await discoverReader.GetLessonInputsAsync(userId, cancellationToken);
            recommended = LearningRecommendationPolicy.GetTopRecommendedLesson(inputs);
        }
        var action = nextActionPolicy.SelectAction(new TodayActionContext(
            dashboard.ActiveSession, dashboard.StudyPlan,
            dashboard.Metrics.ReviewsDue,
            dashboard.Recommendations.FirstOrDefault(item =>
                item.IsResourceAvailable),
            dashboard.ActiveRecommendationCount)
        {
            ContinueLesson = continuing,
            RecommendedLesson = recommended,
            HasActionablePlan = dashboard.HasActionablePlan
        });
        if (recentActivity?.ResourceId == action.Context?.ResourceId) recentActivity = null;
        return new TodayDashboardResult(
            currentUtc, userId, dashboard.DisplayName,
            dashboard.HasCompletedOnboarding, action, recentActivity,
            dashboard.Metrics, dashboard.StudyPlan, dashboard.Recommendations,
            dashboard.WeakTopics, dashboard.WeeklyActivity);
    }
}
