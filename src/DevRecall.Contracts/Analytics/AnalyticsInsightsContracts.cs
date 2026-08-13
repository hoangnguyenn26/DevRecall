namespace DevRecall.Contracts.Analytics;

public sealed record AnalyticsPeriodResponse(DateTimeOffset StartUtc, DateTimeOffset EndUtc);
public sealed record AnalyticsComparisonResponse(long Current, long Previous, long Difference);
public sealed record AnalyticsActivityPointResponse(DateOnly Date, int StudyMinutes, int PracticeCount,
    int LessonsCompleted);
public sealed record AnalyticsRecentActivityResponse(string Type, string Title, DateTimeOffset OccurredAtUtc,
    string? SourceSlug, bool IsSourceAvailable);
public sealed record AnalyticsOverviewResponse(string Range, AnalyticsPeriodResponse Period,
    AnalyticsComparisonResponse StudyMinutes, AnalyticsComparisonResponse ActiveDays,
    AnalyticsComparisonResponse StudySessions, AnalyticsComparisonResponse PracticeActivities,
    int ReviewCount, int InterviewAttemptCount, int DsaAttemptCount, int LearningContentCompletedCount,
    IReadOnlyList<AnalyticsActivityPointResponse> Activity,
    IReadOnlyList<AnalyticsRecentActivityResponse> RecentActivity);
public sealed record RatingDistributionResponse(int First, int Second, int Third, int Fourth, int Total);
public sealed record LearningPerformanceResponse(string Range, AnalyticsPeriodResponse Period,
    RatingDistributionResponse ReviewCurrent, RatingDistributionResponse ReviewPrevious,
    RatingDistributionResponse InterviewCurrent, RatingDistributionResponse InterviewPrevious,
    RatingDistributionResponse DsaCurrent, RatingDistributionResponse DsaPrevious);
