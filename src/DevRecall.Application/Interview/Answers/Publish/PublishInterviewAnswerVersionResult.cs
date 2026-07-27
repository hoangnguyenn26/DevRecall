namespace DevRecall.Application.Interview.Answers.Publish;

public sealed record PublishInterviewAnswerVersionResult(
    Guid Id,
    Guid InterviewQuestionId,
    int VersionNumber,
    string Content,
    string Status,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc,
    DateTimeOffset? PublishedAtUtc);
