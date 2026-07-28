using DevRecall.Application.Common.Exceptions;
using DevRecall.Application.Identity;
using DevRecall.Application.Study.Resources;
using DevRecall.Domain.Study;

namespace DevRecall.Application.Study.GetDetail;

public sealed record GetStudySessionDetailQuery(Guid StudySessionId);
public sealed record StudySessionProgressSummary(
    int TotalItems, int PendingItems, int InProgressItems,
    int CompletedItems, int SkippedItems, double CompletionPercentage,
    int KnowledgeItems, int InterviewItems, int DsaItems, int ReviewItems);
public sealed record StudySessionDetailItem(
    Guid Id, string ResourceType, Guid ResourceId,
    string ResourceTitle, string? ResourcePreview,
    bool IsResourceAvailable, int Position, string Status,
    DateTimeOffset? StartedAtUtc, DateTimeOffset? CompletedAtUtc,
    string? Notes, DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc);
public sealed record GetStudySessionDetailResult(
    Guid Id, string Title, string Status, int PlannedDurationMinutes,
    int? ActualDurationMinutes, DateTimeOffset? StartedAtUtc,
    DateTimeOffset? CompletedAtUtc, string? Notes, int Version,
    DateTimeOffset CreatedAtUtc, DateTimeOffset UpdatedAtUtc,
    StudySessionProgressSummary Progress,
    IReadOnlyList<StudySessionDetailItem> Items);

public sealed class GetStudySessionDetailHandler(
    IStudySessionRepository repository,
    IStudyResourceSummaryReader resourceSummaryReader,
    ICurrentUser currentUser)
{
    public async Task<GetStudySessionDetailResult> HandleAsync(
        GetStudySessionDetailQuery query,
        CancellationToken cancellationToken)
    {
        var userId = StudySessionSupport.GetUserId(currentUser);
        var session = await repository.GetByIdAndUserIdAsync(
            query.StudySessionId, userId, cancellationToken);
        if (session is null)
        {
            throw new NotFoundException(
                StudySessionErrors.SessionNotFound.Code,
                StudySessionErrors.SessionNotFound.Message);
        }

        var ordered = session.Items.OrderBy(item => item.Position)
            .ThenBy(item => item.Id).ToList();
        var summaries = await resourceSummaryReader.ReadManyAsync(
            userId, ordered.Select(item => new StudyResourceReference(
                item.ResourceType, item.ResourceId)).ToArray(),
            cancellationToken);
        var byKey = summaries.ToDictionary(
            summary => (summary.ResourceType, summary.ResourceId));
        var items = ordered.Select(item =>
        {
            byKey.TryGetValue(
                (item.ResourceType, item.ResourceId), out var resource);
            return new StudySessionDetailItem(
                item.Id, item.ResourceType.ToString(), item.ResourceId,
                resource?.Title ?? "Unavailable resource",
                resource?.Preview, resource is not null, item.Position,
                item.Status.ToString(), item.StartedAtUtc,
                item.CompletedAtUtc, item.Notes,
                item.CreatedAtUtc, item.UpdatedAtUtc);
        }).ToList();
        return new GetStudySessionDetailResult(
            session.Id, session.Title, session.Status.ToString(),
            session.PlannedDurationMinutes, session.ActualDurationMinutes,
            session.StartedAtUtc, session.CompletedAtUtc, session.Notes,
            session.Version, session.CreatedAtUtc, session.UpdatedAtUtc,
            BuildProgress(ordered), items);
    }

    private static StudySessionProgressSummary BuildProgress(
        List<StudySessionItem> items)
    {
        var completed = items.Count(item =>
            item.Status == StudySessionItemStatus.Completed);
        var percentage = items.Count == 0
            ? 0
            : Math.Round(completed / (double)items.Count * 100, 2);
        return new StudySessionProgressSummary(
            items.Count,
            items.Count(item =>
                item.Status == StudySessionItemStatus.Pending),
            items.Count(item =>
                item.Status == StudySessionItemStatus.InProgress),
            completed,
            items.Count(item =>
                item.Status == StudySessionItemStatus.Skipped),
            percentage,
            CountType(items, StudyResourceType.KnowledgeNode),
            CountType(items, StudyResourceType.InterviewQuestion),
            CountType(items, StudyResourceType.DsaProblem),
            CountType(items, StudyResourceType.ReviewItem));
    }

    private static int CountType(
        IEnumerable<StudySessionItem> items, StudyResourceType type) =>
        items.Count(item => item.ResourceType == type);
}
