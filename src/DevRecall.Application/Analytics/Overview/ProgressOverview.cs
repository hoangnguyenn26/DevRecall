using DevRecall.Application.Identity;

namespace DevRecall.Application.Analytics.Overview;

public sealed record GetProgressOverviewQuery(
    DateTimeOffset? FromUtc, DateTimeOffset? ToUtc);
public sealed record ProgressOverviewReadModel(
    int StudyMinutes, int CompletedSessions, int CancelledSessions,
    int StudyItemsCompleted, int StudyItemsSkipped,
    int ReviewsCompleted, int DsaAttempts,
    int InterviewItemsCompleted, int KnowledgeItemsCompleted,
    int ActiveStudyDays);
public sealed record GetProgressOverviewResult(
    DateTimeOffset FromUtc, DateTimeOffset ToUtc,
    int StudyMinutes, int CompletedSessions, int CancelledSessions,
    int StudyItemsCompleted, int StudyItemsSkipped,
    int ReviewsCompleted, int DsaAttempts,
    int InterviewItemsCompleted, int KnowledgeItemsCompleted,
    int ActiveStudyDays);

public interface IProgressOverviewReader
{
    Task<ProgressOverviewReadModel> ReadAsync(
        Guid userId, AnalyticsDateRange range,
        CancellationToken cancellationToken);
}

public sealed class GetProgressOverviewHandler(
    IProgressOverviewReader reader,
    AnalyticsDateRangeResolver dateRangeResolver,
    ICurrentUser currentUser)
{
    public async Task<GetProgressOverviewResult> HandleAsync(
        GetProgressOverviewQuery query,
        CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        var range = dateRangeResolver.Resolve(
            new AnalyticsDateRangeInput(query.FromUtc, query.ToUtc));
        var overview = await reader.ReadAsync(
            userId, range, cancellationToken);
        return new GetProgressOverviewResult(
            range.FromUtc, range.ToUtc, overview.StudyMinutes,
            overview.CompletedSessions, overview.CancelledSessions,
            overview.StudyItemsCompleted, overview.StudyItemsSkipped,
            overview.ReviewsCompleted, overview.DsaAttempts,
            overview.InterviewItemsCompleted,
            overview.KnowledgeItemsCompleted,
            overview.ActiveStudyDays);
    }

    private Guid GetUserId()
    {
        if (!currentUser.IsAuthenticated || currentUser.UserId is null)
        {
            throw new Common.Exceptions.UnauthorizedException(
                "IDENTITY_UNAUTHENTICATED",
                "Authentication is required.");
        }

        return currentUser.UserId.Value;
    }
}
