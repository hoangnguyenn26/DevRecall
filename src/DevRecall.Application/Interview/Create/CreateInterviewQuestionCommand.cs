namespace DevRecall.Application.Interview.Create;

public sealed record CreateInterviewQuestionCommand(
    string Title,
    string Question,
    string Topic,
    string Difficulty,
    string? Notes);
