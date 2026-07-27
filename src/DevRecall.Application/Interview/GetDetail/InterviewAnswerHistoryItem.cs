namespace DevRecall.Application.Interview.GetDetail;

public sealed record InterviewAnswerHistoryItem(
    Guid Id,
    int VersionNumber,
    string Status,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc,
    DateTimeOffset? PublishedAtUtc);
