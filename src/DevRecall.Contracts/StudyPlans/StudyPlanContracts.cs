namespace DevRecall.Contracts.StudyPlans;

public sealed record GenerateStudyPlanRequest(
    string Title,
    int TotalDurationMinutes,
    int MaximumCandidates = 100);

public sealed record GenerateStudyPlanResponse(
    Guid StudyPlanId,
    string Title,
    string Status,
    int TotalPlannedDurationMinutes,
    int ItemCount,
    DateTimeOffset GeneratedAtUtc,
    DateTimeOffset? ExpiresAtUtc,
    int Version,
    IReadOnlyList<StudyPlanItemResponse> Items);

public sealed record StudyPlanItemResponse(
    Guid ItemId,
    Guid? SourceRecommendationId,
    string SourceType,
    string ResourceType,
    Guid ResourceId,
    string ResourceTitle,
    string? ResourcePreview,
    bool IsResourceAvailable,
    int PlannedDurationMinutes,
    int Position,
    string? ResourceKey = null, string? ContentType = null, string? SourceName = null, string? ResourceKind = null);

public sealed class GetStudyPlansRequest
{
    public string? Status { get; init; }
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 20;
}

public sealed record StudyPlanListItemResponse(
    Guid StudyPlanId,
    string Title,
    string Status,
    int ItemCount,
    int TotalPlannedDurationMinutes,
    DateTimeOffset GeneratedAtUtc,
    DateTimeOffset? ExpiresAtUtc,
    DateTimeOffset? ReadyAtUtc,
    DateTimeOffset? ConvertedAtUtc,
    Guid? ConvertedStudySessionId,
    DateTimeOffset? CancelledAtUtc,
    DateTimeOffset UpdatedAtUtc,
    int Version);

public sealed record StudyPlanDetailResponse(
    Guid StudyPlanId,
    string Title,
    string Status,
    int ItemCount,
    int TotalPlannedDurationMinutes,
    DateTimeOffset GeneratedAtUtc,
    DateTimeOffset? ExpiresAtUtc,
    DateTimeOffset? ReadyAtUtc,
    DateTimeOffset? ConvertedAtUtc,
    Guid? ConvertedStudySessionId,
    DateTimeOffset? CancelledAtUtc,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc,
    int Version,
    IReadOnlyList<StudyPlanItemResponse> Items);

public sealed record StudyPlanMutationRequest(int ExpectedVersion);
public sealed record UpdateStudyPlanRequest(string Title, int ExpectedVersion);
public sealed record ReplaceStudyPlanDraftItemRequest(
    Guid? ItemId, string ResourceType, Guid ResourceId,
    int PlannedDurationMinutes);
public sealed record ReplaceStudyPlanDraftRequest(
    string Title, IReadOnlyList<ReplaceStudyPlanDraftItemRequest> Items,
    int ExpectedVersion);
public sealed record UpdateStudyPlanItemRequest(
    int PlannedDurationMinutes, int ExpectedVersion);
public sealed record ReorderStudyPlanItemsRequest(
    IReadOnlyList<Guid> ItemIds, int ExpectedVersion);
public sealed record StudyPlanMutationResponse(
    Guid StudyPlanId, string Status, int ItemCount,
    int TotalPlannedDurationMinutes, DateTimeOffset UpdatedAtUtc,
    DateTimeOffset? ReadyAtUtc, DateTimeOffset? CancelledAtUtc, int Version);
public sealed record ConvertStudyPlanRequest(int ExpectedVersion);
public sealed record ConvertStudyPlanResponse(
    Guid StudyPlanId, string StudyPlanStatus, Guid StudySessionId,
    string StudySessionStatus, string Title, int ItemCount,
    int TotalPlannedDurationMinutes, DateTimeOffset ConvertedAtUtc,
    int StudyPlanVersion, int StudySessionVersion);
public sealed record AddLearningContentToStudyPlanRequest(int ExpectedVersion, Guid SubmissionId);
public sealed record AddLearningContentToStudyPlanResponse(Guid StudyPlanId, Guid ItemId,
    string PlanTitle, bool Added, int Version);
public sealed record LearningContentStudyPlanOptionResponse(Guid StudyPlanId, string Title,
    int ItemCount, int TotalPlannedDurationMinutes, int Version, bool AlreadyContains);
