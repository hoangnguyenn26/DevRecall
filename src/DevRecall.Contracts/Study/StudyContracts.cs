namespace DevRecall.Contracts.Study;

public sealed record CreateStudySessionRequest(
    string Title, int PlannedDurationMinutes, string? Notes);

public sealed record UpdateStudySessionRequest(
    string Title, int PlannedDurationMinutes, string? Notes,
    int ExpectedVersion);

public sealed record AddStudySessionItemRequest(
    string ResourceType, Guid ResourceId, string? Notes,
    int ExpectedVersion);

public sealed record ReorderStudySessionItemsRequest(
    IReadOnlyList<Guid> OrderedItemIds, int ExpectedVersion);

public sealed record StartStudySessionRequest(int ExpectedVersion);
public sealed record StartStudySessionItemRequest(int ExpectedVersion);
public sealed record CompleteStudySessionItemRequest(
    string? Notes, int ExpectedVersion, Guid? SubmissionId = null,
    Guid? EvidenceId = null);
public sealed record SkipStudySessionItemRequest(
    string? Notes, int ExpectedVersion, Guid? SubmissionId = null);

public sealed class RemoveStudySessionItemRequest
{
    public int ExpectedVersion { get; init; }
}
public sealed record CompleteStudySessionRequest(int ExpectedVersion);
public sealed record CancelStudySessionRequest(int ExpectedVersion);
public sealed record UpdateStudySessionReflectionRequest(
    string? Reflection, int ExpectedVersion);
public sealed record UpdateStudySessionReflectionResponse(
    Guid Id, string? Reflection, int Version, DateTimeOffset UpdatedAtUtc);

public sealed record StudySessionResponse(
    Guid Id, string Title, string Status, int PlannedDurationMinutes,
    DateTimeOffset? StartedAtUtc, DateTimeOffset? CompletedAtUtc,
    int? ActualDurationMinutes, string? Notes,
    int Version, DateTimeOffset CreatedAtUtc, DateTimeOffset UpdatedAtUtc);

public sealed record StudySessionItemResponse(
    Guid Id, string ResourceType, Guid ResourceId, int Position,
    string Status, DateTimeOffset? StartedAtUtc,
    DateTimeOffset? CompletedAtUtc, string? Notes,
    int Version, DateTimeOffset CreatedAtUtc, DateTimeOffset UpdatedAtUtc);

public sealed record StudySessionItemPositionResponse(Guid Id, int Position);
public sealed record ReorderStudySessionItemsResponse(
    int Version, IReadOnlyList<StudySessionItemPositionResponse> Items);

public sealed record StartStudySessionResponse(
    Guid Id, string Status, DateTimeOffset StartedAtUtc,
    int Version, DateTimeOffset UpdatedAtUtc);

public sealed record StudySessionItemStateResponse(
    Guid Id, string Status, DateTimeOffset? StartedAtUtc,
    DateTimeOffset? CompletedAtUtc, string? Notes,
    int Version, DateTimeOffset UpdatedAtUtc);

public sealed record RemoveStudySessionItemResponse(int Version);

public sealed record StudySessionCompletionSummaryResponse(
    int TotalItems, int PendingItems, int InProgressItems,
    int CompletedItems, int SkippedItems,
    int KnowledgeItemsCompleted, int InterviewItemsCompleted,
    int DsaItemsCompleted, int ReviewItemsCompleted);

public sealed record CompleteStudySessionResponse(
    Guid Id, string Status, DateTimeOffset StartedAtUtc,
    DateTimeOffset CompletedAtUtc, int ActualDurationMinutes,
    int PlannedDurationMinutes, int Version,
    StudySessionCompletionSummaryResponse Summary,
    DateTimeOffset UpdatedAtUtc);

public sealed record CancelStudySessionResponse(
    Guid Id, string Status, DateTimeOffset? StartedAtUtc,
    int Version, DateTimeOffset UpdatedAtUtc);

public sealed class GetStudySessionsRequest
{
    public string? Status { get; init; }
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 20;
}

public sealed record StudySessionListItemResponse(
    Guid Id, string Title, string Status, int PlannedDurationMinutes,
    int? ActualDurationMinutes, DateTimeOffset? StartedAtUtc,
    DateTimeOffset? CompletedAtUtc, int TotalItems,
    int CompletedItems, int SkippedItems, int Version,
    DateTimeOffset CreatedAtUtc, DateTimeOffset UpdatedAtUtc);

public sealed record StudySessionProgressSummaryResponse(
    int TotalItems, int PendingItems, int InProgressItems,
    int CompletedItems, int SkippedItems, double CompletionPercentage,
    int KnowledgeItems, int InterviewItems, int DsaItems, int ReviewItems);

public sealed record StudySessionDetailItemResponse(
    Guid Id, string ResourceType, Guid ResourceId,
    string ResourceTitle, string? ResourcePreview,
    bool IsResourceAvailable, bool HasEvidence, int PlannedDurationMinutes,
    int Position, string Status,
    DateTimeOffset? StartedAtUtc, DateTimeOffset? CompletedAtUtc,
    string? Notes, StudySessionEvidenceResponse? Evidence,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc);

public sealed record StudySessionEvidenceResponse(
    Guid AttemptId, string Kind, string Outcome,
    int DurationSeconds, string? TimeComplexity);

public sealed record StudySessionDetailResponse(
    Guid Id, string Title, string Status, int PlannedDurationMinutes,
    int? ActualDurationMinutes, DateTimeOffset? StartedAtUtc,
    DateTimeOffset? CompletedAtUtc, string? Notes, string? Reflection,
    int Version,
    DateTimeOffset CreatedAtUtc, DateTimeOffset UpdatedAtUtc,
    Guid? CurrentItemId, int RemainingPlannedMinutes,
    StudySessionProgressSummaryResponse Progress,
    IReadOnlyList<StudySessionDetailItemResponse> Items);
