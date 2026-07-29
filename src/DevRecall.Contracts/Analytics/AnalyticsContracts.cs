namespace DevRecall.Contracts.Analytics;

public sealed class AnalyticsDateRangeRequest
{
    public DateTimeOffset? FromUtc { get; init; }
    public DateTimeOffset? ToUtc { get; init; }
}

public sealed record ProgressOverviewResponse(
    DateTimeOffset FromUtc, DateTimeOffset ToUtc,
    int StudyMinutes, int CompletedSessions, int CancelledSessions,
    int StudyItemsCompleted, int StudyItemsSkipped,
    int ReviewsCompleted, int DsaAttempts,
    int InterviewItemsCompleted, int KnowledgeItemsCompleted,
    int ActiveStudyDays);
