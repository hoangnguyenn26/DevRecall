using DevRecall.Api.Authorization;
using DevRecall.Application.Analytics.Overview;
using DevRecall.Contracts.Analytics;

namespace DevRecall.Api.Endpoints.Analytics;

public static class AnalyticsEndpoints
{
    public static IEndpointRouteBuilder MapAnalyticsEndpoints(
        this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/v1/analytics")
            .WithTags("Analytics")
            .RequireAuthorization(AuthorizationPolicies.AuthenticatedUser);
        group.MapGet(
            "/progress-overview",
            async (
                [AsParameters] AnalyticsDateRangeRequest request,
                GetProgressOverviewHandler handler,
                CancellationToken cancellationToken) =>
            {
                var result = await handler.HandleAsync(
                    new GetProgressOverviewQuery(
                        request.FromUtc, request.ToUtc),
                    cancellationToken);
                return Results.Ok(new ProgressOverviewResponse(
                    result.FromUtc, result.ToUtc, result.StudyMinutes,
                    result.CompletedSessions, result.CancelledSessions,
                    result.StudyItemsCompleted,
                    result.StudyItemsSkipped,
                    result.ReviewsCompleted, result.DsaAttempts,
                    result.InterviewItemsCompleted,
                    result.KnowledgeItemsCompleted,
                    result.ActiveStudyDays));
            })
            .WithName("GetAnalyticsProgressOverview")
            .Produces<ProgressOverviewResponse>()
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized);
        return endpoints;
    }
}
