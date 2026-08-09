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
    bool IsResourceAvailable, bool HasEvidence, int PlannedDurationMinutes,
    int Position, string Status,
    DateTimeOffset? StartedAtUtc, DateTimeOffset? CompletedAtUtc,
    string? Notes, StudySessionEvidenceSummary? Evidence,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc);
public sealed record StudySessionEvidenceSummary(
    Guid AttemptId, string Kind, string Outcome,
    int DurationSeconds, string? TimeComplexity);
public interface IStudySessionEvidenceSummaryReader
{
    Task<IReadOnlyDictionary<Guid, StudySessionEvidenceSummary>> ReadManyAsync(
        Guid userId, IReadOnlyCollection<StudySessionEvidenceReference> evidence,
        CancellationToken cancellationToken);
}
public sealed record StudySessionEvidenceReference(
    Guid ItemId, StudyResourceType ResourceType, Guid EvidenceId);
public sealed record GetStudySessionDetailResult(
    Guid Id, string Title, string Status, int PlannedDurationMinutes,
    int? ActualDurationMinutes, DateTimeOffset? StartedAtUtc,
    DateTimeOffset? CompletedAtUtc, string? Notes, string? Reflection,
    int Version,
    DateTimeOffset CreatedAtUtc, DateTimeOffset UpdatedAtUtc,
    Guid? CurrentItemId, int RemainingPlannedMinutes,
    StudySessionProgressSummary Progress,
    IReadOnlyList<StudySessionDetailItem> Items);

public sealed class GetStudySessionDetailHandler(
    IStudySessionRepository repository,
    IStudyResourceSummaryReader resourceSummaryReader,
    IStudySessionEvidenceSummaryReader evidenceSummaryReader,
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
        var evidence = await evidenceSummaryReader.ReadManyAsync(
            userId, ordered.Where(item => item.EvidenceId is not null)
                .Select(item => new StudySessionEvidenceReference(
                    item.Id, item.ResourceType, item.EvidenceId!.Value))
                .ToArray(), cancellationToken);
        var items = ordered.Select(item =>
        {
            byKey.TryGetValue(
                (item.ResourceType, item.ResourceId), out var resource);
            return new StudySessionDetailItem(
                item.Id, item.ResourceType.ToString(), item.ResourceId,
                resource?.Title ?? item.TitleSnapshot,
                resource?.Preview, resource is not null,
                item.EvidenceId is not null,
                item.PlannedDurationMinutes, item.Position,
                item.Status.ToString(), item.StartedAtUtc,
                item.CompletedAtUtc, item.Notes,
                evidence.GetValueOrDefault(item.Id),
                item.CreatedAtUtc, item.UpdatedAtUtc);
        }).ToList();
        var current = ordered.FirstOrDefault(item =>
            item.Status == StudySessionItemStatus.InProgress)
            ?? ordered.FirstOrDefault(item =>
                item.Status == StudySessionItemStatus.Pending);
        var remaining = ordered.Where(item => item.Status is
            StudySessionItemStatus.Pending or StudySessionItemStatus.InProgress)
            .Sum(item => item.PlannedDurationMinutes);
        return new GetStudySessionDetailResult(
            session.Id, session.Title, session.Status.ToString(),
            session.PlannedDurationMinutes, session.ActualDurationMinutes,
            session.StartedAtUtc, session.CompletedAtUtc, session.Notes,
            session.Reflection,
            session.Version, session.CreatedAtUtc, session.UpdatedAtUtc,
            current?.Id, remaining,
            BuildProgress(ordered), items);
    }

    private static StudySessionProgressSummary BuildProgress(
        List<StudySessionItem> items)
    {
        var completed = items.Count(item =>
            item.Status == StudySessionItemStatus.Completed);
        var handled = completed + items.Count(item =>
            item.Status == StudySessionItemStatus.Skipped);
        var percentage = items.Count == 0
            ? 0
            : Math.Round(handled / (double)items.Count * 100, 2);
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
