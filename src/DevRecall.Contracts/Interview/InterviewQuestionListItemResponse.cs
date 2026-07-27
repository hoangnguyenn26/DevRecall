namespace DevRecall.Contracts.Interview;

public sealed record InterviewQuestionListItemResponse(
    Guid Id,
    string Title,
    string Topic,
    string Difficulty,
    DateTimeOffset UpdatedAtUtc);
