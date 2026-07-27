namespace DevRecall.Application.Interview.Answers.CreateDraft;

public sealed record CreateInterviewAnswerDraftResult(
    Guid Id,
    Guid InterviewQuestionId,
    int VersionNumber,
    string Content,
    string Status,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc,
    DateTimeOffset? PublishedAtUtc);
