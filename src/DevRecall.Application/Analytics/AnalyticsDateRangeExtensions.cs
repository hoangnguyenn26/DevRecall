namespace DevRecall.Application.Analytics;

public static class AnalyticsDateRangeExtensions
{
    public static DateOnly GetFirstUtcDate(this AnalyticsDateRange range) =>
        DateOnly.FromDateTime(range.FromUtc.UtcDateTime);

    public static DateOnly GetLastIncludedUtcDate(this AnalyticsDateRange range) =>
        DateOnly.FromDateTime(range.ToUtc.AddTicks(-1).UtcDateTime);
}
