using DevRecall.Domain.Common.Errors;

namespace DevRecall.Domain.Interview.Answers;

public static class InterviewAnswerVersionErrors
{
    public static readonly DomainError NotFound = new(
        "INTERVIEW_ANSWER_VERSION_NOT_FOUND",
        "The interview answer version was not found.");

    public static readonly DomainError Published = new(
        "INTERVIEW_ANSWER_VERSION_PUBLISHED",
        "A published interview answer version cannot be modified.");

    public static readonly DomainError DraftAlreadyExists = new(
        "INTERVIEW_ANSWER_DRAFT_ALREADY_EXISTS",
        "The interview question already has an active draft answer.");

    public static readonly DomainError InvalidVersionNumber = new(
        "INTERVIEW_ANSWER_INVALID_VERSION_NUMBER",
        "The answer version number must be greater than zero.");

    public static readonly DomainError VersionConflict = new(
        "INTERVIEW_ANSWER_VERSION_CONFLICT",
        "The answer version could not be created because the answer history changed.");
}
