namespace DevRecall.Application.Interview.Answers.UpdateDraft;

public sealed record UpdateInterviewAnswerDraftResult(
    Guid Id,
    Guid InterviewQuestionId,
    int VersionNumber,
    string Content,
    string Status,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc,
    DateTimeOffset? PublishedAtUtc);
