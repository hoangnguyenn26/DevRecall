using DevRecall.Application.Analytics;
using DevRecall.Application.Analytics.ReviewPerformance;
using DevRecall.Domain.Reviews;
using DevRecall.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace DevRecall.Infrastructure.Analytics;

internal sealed class ReviewPerformanceReader(DevRecallDbContext dbContext)
    : IReviewPerformanceReader
{
    public async Task<ReviewPerformanceReadModel> ReadAsync(
        Guid userId, AnalyticsDateRange range,
        CancellationToken cancellationToken)
    {
        var query =
            from history in dbContext.ReviewHistories.AsNoTracking()
            join item in dbContext.ReviewItems.AsNoTracking()
                on history.ReviewItemId equals item.Id
            where item.UserId == userId
                && history.ReviewedAtUtc >= range.FromUtc
                && history.ReviewedAtUtc < range.ToUtc
            select history;
        var aggregate = await query.GroupBy(_ => 1)
            .Select(group => new ReviewPerformanceReadModel(
                group.Count(),
                group.Count(history =>
                    history.Evaluation == ReviewEvaluation.Again),
                group.Count(history =>
                    history.Evaluation == ReviewEvaluation.Hard),
                group.Count(history =>
                    history.Evaluation == ReviewEvaluation.Good),
                group.Count(history =>
                    history.Evaluation == ReviewEvaluation.Easy),
                group.Average(history =>
                    (decimal)history.PreviousIntervalDays),
                group.Average(history =>
                    (decimal)history.NextIntervalDays)))
            .SingleOrDefaultAsync(cancellationToken);
        return aggregate ?? new ReviewPerformanceReadModel(
            0, 0, 0, 0, 0, 0m, 0m);
    }
}
