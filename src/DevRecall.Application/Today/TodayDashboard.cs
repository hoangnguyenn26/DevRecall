using DevRecall.Application.Common.Exceptions;
using DevRecall.Application.Common.Time;
using DevRecall.Application.Identity;
using DevRecall.Domain.Recommendations;
using DevRecall.Domain.StudyPlans;
using DevRecall.Domain.WeakTopics;

namespace DevRecall.Application.Today;

public static class TodayDashboardDefaults
{
    public const int WeeklyTargetDays = 5;
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
    decimal PriorityScore, string ResourceTitle);
public sealed record TodayWeakTopicReadModel(
    Guid WeakTopicProfileId, WeakTopicResourceType ResourceType,
    Guid ResourceId, WeaknessLevel Level, decimal Score,
    string ResourceTitle, bool IsResourceAvailable);
public sealed record TodayActivityPointReadModel(
    DateOnly Date, int StudyMinutes, int ActivityCount);
public sealed record ActiveStudySessionCandidate(
    Guid Id, string Title, int RemainingItemCount, int? RemainingMinutes);
public sealed record TodayDashboardReadModel(
    string DisplayName, TodayDashboardMetricsReadModel Metrics,
    TodayStudyPlanReadModel? StudyPlan,
    IReadOnlyList<TodayRecommendationReadModel> Recommendations,
    IReadOnlyList<TodayWeakTopicReadModel> WeakTopics,
    IReadOnlyList<TodayActivityPointReadModel> WeeklyActivity,
    ActiveStudySessionCandidate? ActiveSession,
    int ActiveRecommendationCount);

public interface ITodayDashboardReader
{
    Task<TodayDashboardReadModel> ReadAsync(
        Guid userId, DateTimeOffset currentUtc,
        CancellationToken cancellationToken);
}

public sealed record TodayDashboardResult(
    DateTimeOffset GeneratedAtUtc, Guid UserId, string DisplayName,
    TodayNextAction NextAction, TodayDashboardMetricsReadModel Metrics,
    TodayStudyPlanReadModel? StudyPlan,
    IReadOnlyList<TodayRecommendationReadModel> Recommendations,
    IReadOnlyList<TodayWeakTopicReadModel> WeakTopics,
    IReadOnlyList<TodayActivityPointReadModel> WeeklyActivity);

public sealed class GetTodayDashboardHandler(
    ITodayDashboardReader reader, ITodayNextActionPolicy nextActionPolicy,
    ICurrentUser currentUser, IUtcClock utcClock)
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
        var action = nextActionPolicy.SelectAction(new TodayActionContext(
            dashboard.ActiveSession, dashboard.StudyPlan,
            dashboard.Metrics.ReviewsDue,
            dashboard.Recommendations.Count == 0
                ? null : dashboard.Recommendations[0],
            dashboard.ActiveRecommendationCount));
        return new TodayDashboardResult(
            currentUtc, userId, dashboard.DisplayName, action,
            dashboard.Metrics, dashboard.StudyPlan, dashboard.Recommendations,
            dashboard.WeakTopics, dashboard.WeeklyActivity);
    }
}
