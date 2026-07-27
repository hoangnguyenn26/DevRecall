namespace DevRecall.Contracts.Interview;

public sealed record UpdateInterviewQuestionRequest(
    string Title,
    string Question,
    string Topic,
    string Difficulty,
    string? Notes);
