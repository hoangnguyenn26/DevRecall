namespace DevRecall.Contracts.Interview;

public sealed record InterviewQuestionDetailResponse(
    Guid Id,
    string Title,
    string Question,
    string Topic,
    string Difficulty,
    string? Notes,
    string Status,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc);
