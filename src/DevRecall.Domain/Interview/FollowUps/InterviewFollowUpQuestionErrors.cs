using DevRecall.Domain.Common.Errors;

namespace DevRecall.Domain.Interview.FollowUps;

public static class InterviewFollowUpQuestionErrors
{
    public static readonly DomainError NotFound = new(
        "INTERVIEW_FOLLOW_UP_NOT_FOUND",
        "The interview follow-up question was not found.");

    public static readonly DomainError Archived = new(
        "INTERVIEW_FOLLOW_UP_ARCHIVED",
        "An archived interview follow-up question cannot be modified.");

    public static readonly DomainError InvalidOrder = new(
        "INTERVIEW_FOLLOW_UP_INVALID_ORDER",
        "The interview follow-up question order is invalid.");
}
