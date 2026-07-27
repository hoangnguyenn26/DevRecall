namespace DevRecall.Contracts.Interview.Answers;

public sealed record InterviewAnswerHistoryItemResponse(
    Guid Id,
    int VersionNumber,
    string Status,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc,
    DateTimeOffset? PublishedAtUtc);
