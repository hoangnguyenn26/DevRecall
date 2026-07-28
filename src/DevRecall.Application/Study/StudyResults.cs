using DevRecall.Domain.Study;

namespace DevRecall.Application.Study;

public sealed record StudySessionResult(
    Guid Id, string Title, string Status, int PlannedDurationMinutes,
    DateTimeOffset? StartedAtUtc, DateTimeOffset? CompletedAtUtc,
    int? ActualDurationMinutes, string? Notes,
    DateTimeOffset CreatedAtUtc, DateTimeOffset UpdatedAtUtc);

public sealed record StudySessionItemResult(
    Guid Id, string ResourceType, Guid ResourceId, int Position,
    string Status, DateTimeOffset? StartedAtUtc,
    DateTimeOffset? CompletedAtUtc, string? Notes,
    DateTimeOffset CreatedAtUtc, DateTimeOffset UpdatedAtUtc);

public sealed record StudySessionItemStateResult(
    Guid Id, string Status, DateTimeOffset? StartedAtUtc,
    DateTimeOffset? CompletedAtUtc, string? Notes,
    DateTimeOffset UpdatedAtUtc);

public sealed record StudySessionItemPositionResult(Guid Id, int Position);

internal static class StudyResultMapper
{
    public static StudySessionResult Map(StudySession session) =>
        new(
            session.Id, session.Title, session.Status.ToString(),
            session.PlannedDurationMinutes, session.StartedAtUtc,
            session.CompletedAtUtc, session.ActualDurationMinutes,
            session.Notes, session.CreatedAtUtc, session.UpdatedAtUtc);

    public static StudySessionItemResult Map(StudySessionItem item) =>
        new(
            item.Id, item.ResourceType.ToString(), item.ResourceId,
            item.Position, item.Status.ToString(), item.StartedAtUtc,
            item.CompletedAtUtc, item.Notes, item.CreatedAtUtc,
            item.UpdatedAtUtc);

    public static StudySessionItemStateResult MapState(
        StudySessionItem item) =>
        new(
            item.Id, item.Status.ToString(), item.StartedAtUtc,
            item.CompletedAtUtc, item.Notes, item.UpdatedAtUtc);
}
