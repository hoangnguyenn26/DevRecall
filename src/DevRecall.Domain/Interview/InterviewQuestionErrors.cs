using DevRecall.Domain.Common.Errors;

namespace DevRecall.Domain.Interview;

public static class InterviewQuestionErrors
{
    public static readonly DomainError NotFound = new(
        "INTERVIEW_QUESTION_NOT_FOUND",
        "The interview question was not found.");

    public static readonly DomainError Archived = new(
        "INTERVIEW_QUESTION_ARCHIVED",
        "An archived interview question cannot be modified.");

    public static readonly DomainError InvalidDifficulty = new(
        "INTERVIEW_QUESTION_INVALID_DIFFICULTY",
        "The interview question difficulty is invalid.");
}
