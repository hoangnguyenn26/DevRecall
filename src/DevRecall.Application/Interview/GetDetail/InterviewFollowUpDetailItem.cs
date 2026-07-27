namespace DevRecall.Application.Interview.GetDetail;

public sealed record InterviewFollowUpDetailItem(
    Guid Id,
    string Prompt,
    int SortOrder,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc);
