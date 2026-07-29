using DevRecall.Api.Authorization;
using DevRecall.Application.Analytics.DailyActivity;
using DevRecall.Application.Analytics.DsaPerformance;
using DevRecall.Application.Analytics.ModuleBreakdown;
using DevRecall.Application.Analytics.Overview;
using DevRecall.Application.Analytics.ReviewPerformance;
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
            "/dsa-performance",
            async (
                [AsParameters] AnalyticsDateRangeRequest request,
                GetDsaPerformanceHandler handler,
                CancellationToken cancellationToken) =>
            {
                var result = await handler.HandleAsync(
                    new GetDsaPerformanceQuery(
                        request.FromUtc, request.ToUtc),
                    cancellationToken);
                return Results.Ok(new DsaPerformanceResponse(
                    result.FromUtc, result.ToUtc, result.TotalAttempts,
                    result.SolvedAttempts,
                    result.PartiallySolvedAttempts,
                    result.FailedAttempts, result.SkippedAttempts,
                    result.ProblemsPracticed, result.SolvedRate,
                    result.AverageDurationMinutes));
            })
            .WithName("GetAnalyticsDsaPerformance")
            .Produces<DsaPerformanceResponse>()
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized);
        group.MapGet(
            "/daily-activity",
            async (
                [AsParameters] AnalyticsDateRangeRequest request,
                GetDailyActivityHandler handler,
                CancellationToken cancellationToken) =>
            {
                var result = await handler.HandleAsync(
                    new GetDailyActivityQuery(
                        request.FromUtc, request.ToUtc),
                    cancellationToken);
                return Results.Ok(new DailyActivityResponse(
                    result.FromUtc, result.ToUtc,
                    result.Days.Select(day =>
                        new DailyActivityDayResponse(
                            day.Date, day.StudyMinutes,
                            day.CompletedSessions,
                            day.CompletedStudyItems, day.Reviews,
                            day.DsaAttempts)).ToList()));
            })
            .WithName("GetAnalyticsDailyActivity")
            .Produces<DailyActivityResponse>()
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized);
        group.MapGet(
            "/module-breakdown",
            async (
                [AsParameters] AnalyticsDateRangeRequest request,
                GetModuleBreakdownHandler handler,
                CancellationToken cancellationToken) =>
            {
                var result = await handler.HandleAsync(
                    new GetModuleBreakdownQuery(
                        request.FromUtc, request.ToUtc),
                    cancellationToken);
                return Results.Ok(new ModuleBreakdownResponse(
                    result.FromUtc, result.ToUtc,
                    result.TotalCompletedItems,
                    result.Modules.Select(module =>
                        new ModuleBreakdownItemResponse(
                            module.ResourceType, module.CompletedItems,
                            module.Percentage)).ToList()));
            })
            .WithName("GetAnalyticsModuleBreakdown")
            .Produces<ModuleBreakdownResponse>()
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized);
        group.MapGet(
            "/review-performance",
            async (
                [AsParameters] AnalyticsDateRangeRequest request,
                GetReviewPerformanceHandler handler,
                CancellationToken cancellationToken) =>
            {
                var result = await handler.HandleAsync(
                    new GetReviewPerformanceQuery(
                        request.FromUtc, request.ToUtc),
                    cancellationToken);
                return Results.Ok(new ReviewPerformanceResponse(
                    result.FromUtc, result.ToUtc, result.TotalReviews,
                    result.AgainCount, result.HardCount, result.GoodCount,
                    result.EasyCount, result.SuccessRate,
                    result.AveragePreviousIntervalDays,
                    result.AverageNextIntervalDays));
            })
            .WithName("GetAnalyticsReviewPerformance")
            .Produces<ReviewPerformanceResponse>()
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized);
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
