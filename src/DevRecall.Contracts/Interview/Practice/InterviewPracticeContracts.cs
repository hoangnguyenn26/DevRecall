namespace DevRecall.Contracts.Interview.Practice;

public sealed record InterviewReferenceAnswerResponse(Guid AnswerId, string Content, int Version);
public sealed record InterviewPracticeFollowUpResponse(Guid FollowUpId, string Question, string? ReferenceAnswer);
public sealed record GetInterviewPracticeResponse(
    Guid QuestionId, string Question, string? Category, string? Difficulty,
    InterviewReferenceAnswerResponse? ReferenceAnswer,
    IReadOnlyList<InterviewPracticeFollowUpResponse> FollowUps, int QuestionVersion);
public sealed record InterviewFollowUpAttemptRequest(Guid FollowUpId, string Answer);
public sealed record CompleteInterviewPracticeRequest(
    string Answer, string SelfRating, IReadOnlyList<InterviewFollowUpAttemptRequest> FollowUps,
    DateTimeOffset StartedAtUtc, Guid SubmissionId,
    Guid? ReferenceAnswerId = null);
public sealed record CompleteInterviewPracticeResponse(
    Guid AttemptId, Guid QuestionId, string SelfRating, int FollowUpsAnswered,
    int FollowUpsSkipped, int DurationSeconds, DateTimeOffset CompletedAtUtc);
public sealed record GetInterviewPracticeHistoryRequest(int Page = 1, int PageSize = 10);
public sealed record InterviewPracticeAttemptSummaryResponse(
    Guid AttemptId, string SelfRating, int DurationSeconds,
    int FollowUpsAnswered, DateTimeOffset CompletedAtUtc);
public sealed record GetInterviewPracticeHistoryResponse(
    IReadOnlyList<InterviewPracticeAttemptSummaryResponse> Items,
    int Page, int PageSize, int TotalCount, int TotalPages);
public sealed record InterviewFollowUpAttemptResponse(
    Guid FollowUpId, string QuestionSnapshot, string AnswerSnapshot);
public sealed record GetInterviewPracticeAttemptResponse(
    Guid AttemptId, Guid QuestionId, string QuestionSnapshot,
    string AnswerSnapshot, string? ReferenceAnswerSnapshot, string SelfRating,
    IReadOnlyList<InterviewFollowUpAttemptResponse> FollowUps,
    DateTimeOffset StartedAtUtc, DateTimeOffset CompletedAtUtc, int DurationSeconds);
