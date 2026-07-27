namespace DevRecall.Contracts.Interview.FollowUps;

public sealed record InterviewFollowUpItemResponse(
    Guid Id,
    string Prompt,
    int SortOrder,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc);
