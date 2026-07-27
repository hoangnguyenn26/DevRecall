using DevRecall.Contracts.Interview.Answers;
using DevRecall.Contracts.Interview.FollowUps;

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
    DateTimeOffset UpdatedAtUtc,
    InterviewAnswerSummaryResponse? CurrentPublishedAnswer,
    InterviewAnswerSummaryResponse? LatestDraft,
    IReadOnlyList<InterviewAnswerHistoryItemResponse> AnswerHistory,
    IReadOnlyList<InterviewFollowUpItemResponse> FollowUps);
