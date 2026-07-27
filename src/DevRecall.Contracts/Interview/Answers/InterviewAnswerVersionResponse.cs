namespace DevRecall.Contracts.Interview.Answers;

public sealed record InterviewAnswerVersionResponse(
    Guid Id,
    Guid InterviewQuestionId,
    int VersionNumber,
    string Content,
    string Status,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc,
    DateTimeOffset? PublishedAtUtc);
