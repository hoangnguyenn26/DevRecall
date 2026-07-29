namespace DevRecall.Application.Analytics;

public sealed record AnalyticsDateRange(
    DateTimeOffset FromUtc, DateTimeOffset ToUtc)
{
    public TimeSpan Duration => ToUtc - FromUtc;
}

public sealed record AnalyticsDateRangeInput(
    DateTimeOffset? FromUtc, DateTimeOffset? ToUtc);
