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
    DateTimeOffset StartedAtUtc, Guid SubmissionId);
public sealed record CompleteInterviewPracticeResponse(
    Guid AttemptId, Guid QuestionId, string SelfRating, int FollowUpsAnswered,
    int FollowUpsSkipped, int DurationSeconds, DateTimeOffset CompletedAtUtc);
public sealed record InterviewPracticeAttemptListItemResponse(
    Guid AttemptId, string QuestionSnapshot, string SelfRating,
    int FollowUpsAnswered, int DurationSeconds, DateTimeOffset CompletedAtUtc);
