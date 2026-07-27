namespace DevRecall.Application.Interview.Answers.UpdateDraft;

public sealed record UpdateInterviewAnswerDraftCommand(
    Guid InterviewQuestionId,
    Guid AnswerVersionId,
    string Content);
