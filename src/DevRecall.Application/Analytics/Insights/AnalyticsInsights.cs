using DevRecall.Application.Common.Exceptions;
using DevRecall.Application.Common.Time;
using DevRecall.Application.Identity;

namespace DevRecall.Application.Analytics.Insights;

public enum AnalyticsRange { SevenDays = 7, ThirtyDays = 30, NinetyDays = 90 }
public sealed record AnalyticsComparison(long Current, long Previous) { public long Difference => Current - Previous; }
public sealed record AnalyticsPeriod(DateTimeOffset StartUtc, DateTimeOffset EndUtc);
public sealed record AnalyticsOverviewAggregate(int StudyMinutes, int ActiveDays, int Sessions, int Reviews, int Interviews, int DsaAttempts);
public sealed record AnalyticsActivityPoint(DateOnly Date, int StudyMinutes, int PracticeCount);
public sealed record AnalyticsOverviewResult(string Range, AnalyticsPeriod Period,
    AnalyticsComparison StudyMinutes, AnalyticsComparison ActiveDays,
    AnalyticsComparison Sessions, AnalyticsComparison Practice,
    int ReviewCount, int InterviewCount, int DsaAttemptCount,
    IReadOnlyList<AnalyticsActivityPoint> Activity);
public sealed record RatingDistribution(int First, int Second, int Third, int Fourth) { public int Total => First + Second + Third + Fourth; }
public sealed record AnalyticsPerformanceResult(string Range, AnalyticsPeriod Period,
    RatingDistribution ReviewCurrent, RatingDistribution ReviewPrevious,
    RatingDistribution InterviewCurrent, RatingDistribution InterviewPrevious,
    RatingDistribution DsaCurrent, RatingDistribution DsaPrevious);

public interface IAnalyticsInsightsReader
{
    Task<AnalyticsOverviewAggregate> ReadOverviewAsync(Guid userId, AnalyticsDateRange range, CancellationToken cancellationToken);
    Task<IReadOnlyList<AnalyticsActivityPoint>> ReadActivityAsync(Guid userId, AnalyticsDateRange range, CancellationToken cancellationToken);
    Task<(RatingDistribution Review, RatingDistribution Interview, RatingDistribution Dsa)> ReadPerformanceAsync(Guid userId, AnalyticsDateRange range, CancellationToken cancellationToken);
}

public sealed class AnalyticsInsightsHandler(IAnalyticsInsightsReader reader, ICurrentUser currentUser, IUtcClock clock)
{
    public async Task<AnalyticsOverviewResult> GetOverviewAsync(string? value, CancellationToken cancellationToken)
    {
        var (name, current, previous) = Resolve(value);
        var userId = UserId();
        var currentData = await reader.ReadOverviewAsync(userId, current, cancellationToken);
        var previousData = await reader.ReadOverviewAsync(userId, previous, cancellationToken);
        var activity = await reader.ReadActivityAsync(userId, current, cancellationToken);
        var practice = currentData.Reviews + currentData.Interviews + currentData.DsaAttempts;
        var previousPractice = previousData.Reviews + previousData.Interviews + previousData.DsaAttempts;
        return new(name, new(current.FromUtc, current.ToUtc),
            new(currentData.StudyMinutes, previousData.StudyMinutes), new(currentData.ActiveDays, previousData.ActiveDays),
            new(currentData.Sessions, previousData.Sessions), new(practice, previousPractice),
            currentData.Reviews, currentData.Interviews, currentData.DsaAttempts, activity);
    }

    public async Task<AnalyticsPerformanceResult> GetPerformanceAsync(string? value, CancellationToken cancellationToken)
    {
        var (name, current, previous) = Resolve(value);
        var userId = UserId();
        var currentData = await reader.ReadPerformanceAsync(userId, current, cancellationToken);
        var previousData = await reader.ReadPerformanceAsync(userId, previous, cancellationToken);
        return new(name, new(current.FromUtc, current.ToUtc), currentData.Review, previousData.Review,
            currentData.Interview, previousData.Interview, currentData.Dsa, previousData.Dsa);
    }

    private (string Name, AnalyticsDateRange Current, AnalyticsDateRange Previous) Resolve(string? value)
    {
        var normalized = value ?? "7d";
        var days = normalized switch { "7d" => 7, "30d" => 30, "90d" => 90, _ => throw new ValidationException(new Dictionary<string, string[]> { ["range"] = ["Range must be 7d, 30d, or 90d."] }) };
        var end = new DateTimeOffset(
            clock.UtcNow.UtcDateTime.Date.AddDays(1), TimeSpan.Zero);
        var start = end.AddDays(-days);
        return (normalized, new(start, end), new(start.AddDays(-days), start));
    }

    private Guid UserId() => currentUser.UserId ?? throw new UnauthorizedException("AUTH_REQUIRED", "Authentication is required.");
}
