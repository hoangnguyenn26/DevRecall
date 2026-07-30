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
    int Position);

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
