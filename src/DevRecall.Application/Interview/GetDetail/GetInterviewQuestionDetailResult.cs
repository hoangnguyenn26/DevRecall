namespace DevRecall.Application.Interview.GetDetail;

public sealed record GetInterviewQuestionDetailResult(
    Guid Id,
    string Title,
    string Question,
    string Topic,
    string Difficulty,
    string? Notes,
    string Status,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc,
    InterviewAnswerDetailItem? CurrentPublishedAnswer,
    InterviewAnswerDetailItem? LatestDraft,
    IReadOnlyList<InterviewAnswerHistoryItem> AnswerHistory,
    IReadOnlyList<InterviewFollowUpDetailItem> FollowUps);
