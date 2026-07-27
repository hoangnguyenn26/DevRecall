namespace DevRecall.Application.Interview.Answers.CreateDraft;

public sealed record CreateInterviewAnswerDraftCommand(
    Guid InterviewQuestionId,
    string Content);
