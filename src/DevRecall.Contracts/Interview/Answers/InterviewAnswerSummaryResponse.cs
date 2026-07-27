namespace DevRecall.Contracts.Interview.Answers;

public sealed record InterviewAnswerSummaryResponse(
    Guid Id,
    int VersionNumber,
    string Content,
    string Status,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc,
    DateTimeOffset? PublishedAtUtc);
