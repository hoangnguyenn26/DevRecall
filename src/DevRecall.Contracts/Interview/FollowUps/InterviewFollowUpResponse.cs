namespace DevRecall.Contracts.Interview.FollowUps;

public sealed record InterviewFollowUpResponse(
    Guid Id,
    Guid InterviewQuestionId,
    string Prompt,
    int SortOrder,
    string Status,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc);
