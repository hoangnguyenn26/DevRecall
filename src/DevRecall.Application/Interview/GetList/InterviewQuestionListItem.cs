namespace DevRecall.Application.Interview.GetList;

public sealed record InterviewQuestionListItem(
    Guid Id,
    string Title,
    string Topic,
    string Difficulty,
    DateTimeOffset UpdatedAtUtc);
