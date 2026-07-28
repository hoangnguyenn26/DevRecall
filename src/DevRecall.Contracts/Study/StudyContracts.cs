namespace DevRecall.Contracts.Study;

public sealed record CreateStudySessionRequest(
    string Title, int PlannedDurationMinutes, string? Notes);

public sealed record UpdateStudySessionRequest(
    string Title, int PlannedDurationMinutes, string? Notes);

public sealed record AddStudySessionItemRequest(
    string ResourceType, Guid ResourceId, string? Notes);

public sealed record ReorderStudySessionItemsRequest(
    IReadOnlyList<Guid> OrderedItemIds);

public sealed record CompleteStudySessionItemRequest(string? Notes);
public sealed record SkipStudySessionItemRequest(string? Notes);

public sealed record StudySessionResponse(
    Guid Id, string Title, string Status, int PlannedDurationMinutes,
    DateTimeOffset? StartedAtUtc, DateTimeOffset? CompletedAtUtc,
    int? ActualDurationMinutes, string? Notes,
    DateTimeOffset CreatedAtUtc, DateTimeOffset UpdatedAtUtc);

public sealed record StudySessionItemResponse(
    Guid Id, string ResourceType, Guid ResourceId, int Position,
    string Status, DateTimeOffset? StartedAtUtc,
    DateTimeOffset? CompletedAtUtc, string? Notes,
    DateTimeOffset CreatedAtUtc, DateTimeOffset UpdatedAtUtc);

public sealed record StudySessionItemPositionResponse(Guid Id, int Position);
public sealed record ReorderStudySessionItemsResponse(
    IReadOnlyList<StudySessionItemPositionResponse> Items);

public sealed record StartStudySessionResponse(
    Guid Id, string Status, DateTimeOffset StartedAtUtc,
    DateTimeOffset UpdatedAtUtc);

public sealed record StudySessionItemStateResponse(
    Guid Id, string Status, DateTimeOffset? StartedAtUtc,
    DateTimeOffset? CompletedAtUtc, string? Notes,
    DateTimeOffset UpdatedAtUtc);
