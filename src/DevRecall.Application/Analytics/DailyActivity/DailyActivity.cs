using DevRecall.Application.Common.Exceptions;
using DevRecall.Application.Identity;

namespace DevRecall.Application.Analytics.DailyActivity;

public sealed record GetDailyActivityQuery(
    DateTimeOffset? FromUtc, DateTimeOffset? ToUtc);
public sealed record DailyActivityAggregate(
    DateOnly Date, int StudyMinutes, int CompletedSessions,
    int CompletedStudyItems, int Reviews, int DsaAttempts);
public sealed record DailyActivityDay(
    DateOnly Date, int StudyMinutes, int CompletedSessions,
    int CompletedStudyItems, int Reviews, int DsaAttempts);
public sealed record GetDailyActivityResult(
    DateTimeOffset FromUtc, DateTimeOffset ToUtc,
    IReadOnlyList<DailyActivityDay> Days);

public interface IDailyActivityReader
{
    Task<IReadOnlyList<DailyActivityAggregate>> ReadAsync(
        Guid userId, AnalyticsDateRange range,
        CancellationToken cancellationToken);
}

public sealed class GetDailyActivityHandler(
    AnalyticsDateRangeResolver dateRangeResolver,
    IDailyActivityReader reader,
    ICurrentUser currentUser)
{
    public async Task<GetDailyActivityResult> HandleAsync(
        GetDailyActivityQuery query,
        CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        var range = dateRangeResolver.Resolve(
            new AnalyticsDateRangeInput(query.FromUtc, query.ToUtc));
        var aggregates = await reader.ReadAsync(
            userId, range, cancellationToken);
        var byDate = aggregates.ToDictionary(item => item.Date);
        var days = new List<DailyActivityDay>();
        var firstDate = range.GetFirstUtcDate();
        var lastDate = range.GetLastIncludedUtcDate();
        for (var date = firstDate; date <= lastDate; date = date.AddDays(1))
        {
            if (byDate.TryGetValue(date, out var aggregate))
            {
                days.Add(new DailyActivityDay(
                    aggregate.Date, aggregate.StudyMinutes,
                    aggregate.CompletedSessions,
                    aggregate.CompletedStudyItems, aggregate.Reviews,
                    aggregate.DsaAttempts));
            }
            else
            {
                days.Add(new DailyActivityDay(date, 0, 0, 0, 0, 0));
            }
        }

        return new GetDailyActivityResult(
            range.FromUtc, range.ToUtc, days);
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
