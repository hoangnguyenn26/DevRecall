namespace DevRecall.Application.Interview.Answers.Publish;

public sealed record PublishInterviewAnswerVersionCommand(
    Guid InterviewQuestionId,
    Guid AnswerVersionId);
