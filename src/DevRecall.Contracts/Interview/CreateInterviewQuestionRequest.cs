namespace DevRecall.Contracts.Interview;

public sealed record CreateInterviewQuestionRequest(
    string Title,
    string Question,
    string Topic,
    string Difficulty,
    string? Notes);
