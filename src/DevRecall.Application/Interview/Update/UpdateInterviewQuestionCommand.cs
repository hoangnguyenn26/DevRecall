namespace DevRecall.Application.Interview.Update;

public sealed record UpdateInterviewQuestionCommand(
    Guid Id,
    string Title,
    string Question,
    string Topic,
    string Difficulty,
    string? Notes);
