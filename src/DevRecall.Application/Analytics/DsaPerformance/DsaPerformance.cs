using DevRecall.Application.Common.Exceptions;
using DevRecall.Application.Identity;

namespace DevRecall.Application.Analytics.DsaPerformance;

public sealed record GetDsaPerformanceQuery(
    DateTimeOffset? FromUtc, DateTimeOffset? ToUtc);
public sealed record DsaPerformanceReadModel(
    int TotalAttempts, int SolvedAttempts,
    int PartiallySolvedAttempts, int FailedAttempts,
    int SkippedAttempts, int ProblemsPracticed,
    decimal AverageDurationMinutes);
public sealed record GetDsaPerformanceResult(
    DateTimeOffset FromUtc, DateTimeOffset ToUtc,
    int TotalAttempts, int SolvedAttempts,
    int PartiallySolvedAttempts, int FailedAttempts,
    int SkippedAttempts, int ProblemsPracticed,
    decimal SolvedRate, decimal AverageDurationMinutes);

public interface IDsaPerformanceReader
{
    Task<DsaPerformanceReadModel> ReadAsync(
        Guid userId, AnalyticsDateRange range,
        CancellationToken cancellationToken);
}

public sealed class GetDsaPerformanceHandler(
    AnalyticsDateRangeResolver dateRangeResolver,
    IDsaPerformanceReader reader,
    ICurrentUser currentUser)
{
    public async Task<GetDsaPerformanceResult> HandleAsync(
        GetDsaPerformanceQuery query,
        CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        var range = dateRangeResolver.Resolve(
            new AnalyticsDateRangeInput(query.FromUtc, query.ToUtc));
        var performance = await reader.ReadAsync(
            userId, range, cancellationToken);
        return new GetDsaPerformanceResult(
            range.FromUtc, range.ToUtc, performance.TotalAttempts,
            performance.SolvedAttempts,
            performance.PartiallySolvedAttempts,
            performance.FailedAttempts, performance.SkippedAttempts,
            performance.ProblemsPracticed,
            AnalyticsMath.Percentage(
                performance.SolvedAttempts, performance.TotalAttempts),
            AnalyticsMath.RoundAverage(
                performance.AverageDurationMinutes));
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
