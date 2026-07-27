namespace DevRecall.Application.Interview.GetDetail;

public sealed record InterviewAnswerDetailItem(
    Guid Id,
    int VersionNumber,
    string Content,
    string Status,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc,
    DateTimeOffset? PublishedAtUtc);
