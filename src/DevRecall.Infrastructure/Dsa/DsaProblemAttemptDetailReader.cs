using DevRecall.Application.Dsa.Attempts;
using DevRecall.Application.Dsa.GetDetail;
using DevRecall.Domain.Dsa.Attempts;
using DevRecall.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace DevRecall.Infrastructure.Dsa;

internal sealed class DsaProblemAttemptDetailReader(
    DevRecallDbContext dbContext)
    : IDsaProblemAttemptDetailReader
{
    public async Task<DsaProblemAttemptDetailData> ReadAsync(
        Guid dsaProblemId, int recentAttemptCount,
        CancellationToken cancellationToken)
    {
        var summary = await ReadSummaryAsync(
            dsaProblemId, cancellationToken);
        var recent = await ReadRecentAsync(
            dsaProblemId, recentAttemptCount, cancellationToken);
        var latestSuccessful = await ReadLatestSuccessfulAsync(
            dsaProblemId, cancellationToken);
        return new DsaProblemAttemptDetailData(
            summary, recent.Count == 0 ? null : recent[0],
            latestSuccessful, recent);
    }

    private async Task<DsaAttemptSummaryReadModel> ReadSummaryAsync(
        Guid dsaProblemId, CancellationToken cancellationToken)
    {
        var summary = await dbContext.DsaAttempts
            .AsNoTracking()
            .Where(attempt => attempt.DsaProblemId == dsaProblemId)
            .GroupBy(_ => 1)
            .Select(group => new DsaAttemptSummaryReadModel(
                group.Count(),
                group.Count(attempt =>
                    attempt.Result == DsaAttemptResult.Solved),
                group.Count(attempt =>
                    attempt.Result == DsaAttemptResult.PartiallySolved),
                group.Count(attempt =>
                    attempt.Result == DsaAttemptResult.Failed),
                group.Count(attempt =>
                    attempt.Result == DsaAttemptResult.Skipped),
                group.Sum(attempt => attempt.DurationMinutes),
                group.Max(attempt =>
                    (DateTimeOffset?)attempt.AttemptedAtUtc)))
            .SingleOrDefaultAsync(cancellationToken);
        return summary ?? new DsaAttemptSummaryReadModel(
            0, 0, 0, 0, 0, 0, null);
    }

    private async Task<IReadOnlyList<DsaAttemptOverviewReadItem>>
        ReadRecentAsync(
            Guid dsaProblemId, int take,
            CancellationToken cancellationToken) =>
        await dbContext.DsaAttempts
            .AsNoTracking()
            .Where(attempt => attempt.DsaProblemId == dsaProblemId)
            .OrderByDescending(attempt => attempt.AttemptNumber)
            .ThenBy(attempt => attempt.Id)
            .Take(take)
            .Select(attempt => new DsaAttemptOverviewReadItem(
                attempt.Id, attempt.AttemptNumber, attempt.Result,
                attempt.Language, attempt.TimeComplexity,
                attempt.SpaceComplexity, attempt.DurationMinutes,
                attempt.AttemptedAtUtc))
            .ToListAsync(cancellationToken);

    private Task<DsaAttemptOverviewReadItem?> ReadLatestSuccessfulAsync(
        Guid dsaProblemId, CancellationToken cancellationToken) =>
        dbContext.DsaAttempts
            .AsNoTracking()
            .Where(attempt => attempt.DsaProblemId == dsaProblemId
                && attempt.Result == DsaAttemptResult.Solved)
            .OrderByDescending(attempt => attempt.AttemptNumber)
            .Select(attempt => new DsaAttemptOverviewReadItem(
                attempt.Id, attempt.AttemptNumber, attempt.Result,
                attempt.Language, attempt.TimeComplexity,
                attempt.SpaceComplexity, attempt.DurationMinutes,
                attempt.AttemptedAtUtc))
            .FirstOrDefaultAsync(cancellationToken);
}
