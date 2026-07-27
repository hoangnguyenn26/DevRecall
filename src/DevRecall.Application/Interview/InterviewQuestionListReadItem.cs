using DevRecall.Domain.Interview;

namespace DevRecall.Application.Interview;

public sealed record InterviewQuestionListReadItem(
    Guid Id,
    string Title,
    string Topic,
    InterviewQuestionDifficulty Difficulty,
    DateTimeOffset UpdatedAtUtc);
