using DevRecall.Api.Authorization;
using DevRecall.Application.Today;
using DevRecall.Contracts.Today;

namespace DevRecall.Api.Endpoints.Today;

public static class TodayEndpoints
{
    public static IEndpointRouteBuilder MapTodayEndpoints(
        this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/v1/today")
            .WithTags("Today")
            .RequireAuthorization(AuthorizationPolicies.AuthenticatedUser);
        group.MapGet(
            "/",
            async (
                GetTodayDashboardHandler handler,
                CancellationToken cancellationToken) =>
            {
                var result = await handler.HandleAsync(
                    new GetTodayDashboardQuery(), cancellationToken);
                return Results.Ok(Map(result));
            })
            .WithName("GetTodayDashboard")
            .Produces<GetTodayDashboardResponse>()
            .ProducesProblem(StatusCodes.Status401Unauthorized);
        return endpoints;
    }

    private static GetTodayDashboardResponse Map(TodayDashboardResult result) =>
        new(
            result.GeneratedAtUtc,
            new(result.UserId, result.DisplayName),
            new(
                result.NextAction.Type.ToString(), result.NextAction.Title,
                result.NextAction.Description, result.NextAction.ActionLabel,
                result.NextAction.TargetPath, result.NextAction.Icon,
                result.NextAction.Context is null ? null : new(
                    result.NextAction.Context.ResourceId,
                    result.NextAction.Context.ResourceType,
                    result.NextAction.Context.ResourceTitle,
                    result.NextAction.Context.PlannedDurationMinutes,
                    result.NextAction.Context.RemainingCount,
                    result.NextAction.Context.Priority)),
            new(
                result.Metrics.ReviewsDue,
                result.Metrics.StudyMinutesThisWeek,
                result.Metrics.ActiveDaysThisWeek,
                result.Metrics.WeeklyTargetDays,
                result.Metrics.WeeklyProgressPercent),
            result.StudyPlan is null ? null : new(
                result.StudyPlan.StudyPlanId, result.StudyPlan.Title,
                result.StudyPlan.Status.ToString(), result.StudyPlan.ItemCount,
                result.StudyPlan.TotalPlannedDurationMinutes,
                result.StudyPlan.Version,
                result.StudyPlan.Items.Select(item => new TodayStudyPlanItemResponse(
                    item.ItemId, item.ResourceType, item.ResourceId,
                    item.ResourceTitle, item.IsResourceAvailable,
                    item.PlannedDurationMinutes, item.Position)).ToArray()),
            result.Recommendations.Select(item => new TodayRecommendationResponse(
                item.RecommendationId, item.ResourceType.ToString(),
                item.ResourceId, item.Type.ToString(), item.Priority.ToString(),
                item.PriorityScore, item.ResourceTitle)).ToArray(),
            result.WeakTopics.Select(item => new TodayWeakTopicResponse(
                item.WeakTopicProfileId, item.ResourceType.ToString(),
                item.ResourceId, item.Level.ToString(), item.Score,
                item.ResourceTitle, item.IsResourceAvailable)).ToArray(),
            result.WeeklyActivity.Select(item => new TodayActivityPointResponse(
                item.Date, item.StudyMinutes, item.ActivityCount)).ToArray());
}
