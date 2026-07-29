using DevRecall.Application.Common.Exceptions;
using DevRecall.Application.Identity;

namespace DevRecall.Application.Analytics.ReviewPerformance;

public sealed record GetReviewPerformanceQuery(
    DateTimeOffset? FromUtc, DateTimeOffset? ToUtc);
public sealed record ReviewPerformanceReadModel(
    int TotalReviews, int AgainCount, int HardCount, int GoodCount,
    int EasyCount, decimal AveragePreviousIntervalDays,
    decimal AverageNextIntervalDays);
public sealed record GetReviewPerformanceResult(
    DateTimeOffset FromUtc, DateTimeOffset ToUtc,
    int TotalReviews, int AgainCount, int HardCount, int GoodCount,
    int EasyCount, decimal SuccessRate,
    decimal AveragePreviousIntervalDays,
    decimal AverageNextIntervalDays);

public interface IReviewPerformanceReader
{
    Task<ReviewPerformanceReadModel> ReadAsync(
        Guid userId, AnalyticsDateRange range,
        CancellationToken cancellationToken);
}

public sealed class GetReviewPerformanceHandler(
    AnalyticsDateRangeResolver dateRangeResolver,
    IReviewPerformanceReader reader,
    ICurrentUser currentUser)
{
    public async Task<GetReviewPerformanceResult> HandleAsync(
        GetReviewPerformanceQuery query,
        CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        var range = dateRangeResolver.Resolve(
            new AnalyticsDateRangeInput(query.FromUtc, query.ToUtc));
        var performance = await reader.ReadAsync(
            userId, range, cancellationToken);
        return new GetReviewPerformanceResult(
            range.FromUtc, range.ToUtc, performance.TotalReviews,
            performance.AgainCount, performance.HardCount,
            performance.GoodCount, performance.EasyCount,
            AnalyticsMath.Percentage(
                performance.GoodCount + performance.EasyCount,
                performance.TotalReviews),
            AnalyticsMath.RoundAverage(
                performance.AveragePreviousIntervalDays),
            AnalyticsMath.RoundAverage(
                performance.AverageNextIntervalDays));
    }

    private Guid GetCurrentUserId()
    {
        if (!currentUser.IsAuthenticated || currentUser.UserId is null)
        {
            throw new UnauthorizedException(
                "IDENTITY_UNAUTHENTICATED",
                "Authentication is required.");
        }

        return currentUser.UserId.Value;
    }
}
