using DevRecall.Application.Analytics;
using DevRecall.Application.Analytics.DsaPerformance;
using DevRecall.Domain.Dsa.Attempts;
using DevRecall.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace DevRecall.Infrastructure.Analytics;

internal sealed class DsaPerformanceReader(DevRecallDbContext dbContext)
    : IDsaPerformanceReader
{
    public async Task<DsaPerformanceReadModel> ReadAsync(
        Guid userId, AnalyticsDateRange range,
        CancellationToken cancellationToken)
    {
        var query =
            from attempt in dbContext.DsaAttempts.AsNoTracking()
            join problem in dbContext.DsaProblems.AsNoTracking()
                on attempt.DsaProblemId equals problem.Id
            where problem.UserId == userId
                && attempt.AttemptedAtUtc >= range.FromUtc
                && attempt.AttemptedAtUtc < range.ToUtc
            select attempt;
        var aggregate = await query.GroupBy(_ => 1)
            .Select(group => new DsaPerformanceReadModel(
                group.Count(),
                group.Count(attempt =>
                    attempt.Result == DsaAttemptResult.Solved),
                group.Count(attempt =>
                    attempt.Result == DsaAttemptResult.PartiallySolved),
                group.Count(attempt =>
                    attempt.Result == DsaAttemptResult.Failed),
                group.Count(attempt =>
                    attempt.Result == DsaAttemptResult.Skipped),
                group.Select(attempt => attempt.DsaProblemId)
                    .Distinct().Count(),
                group.Average(attempt =>
                    (decimal)attempt.DurationMinutes)))
            .SingleOrDefaultAsync(cancellationToken);
        return aggregate ?? new DsaPerformanceReadModel(
            0, 0, 0, 0, 0, 0, 0m);
    }
}
