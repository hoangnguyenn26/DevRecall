using DevRecall.Domain.Common.Errors;

namespace DevRecall.Domain.Reviews;

public static class ReviewErrors
{
    public static readonly DomainError ItemNotFound = new(
        "REVIEW_ITEM_NOT_FOUND", "The review item was not found.");

    public static readonly DomainError ItemAlreadyExists = new(
        "REVIEW_ITEM_ALREADY_EXISTS",
        "An active review item already exists for this resource.");

    public static readonly DomainError ItemArchived = new(
        "REVIEW_ITEM_ARCHIVED", "An archived review item cannot be evaluated.");

    public static readonly DomainError ResourceNotFound = new(
        "REVIEW_RESOURCE_NOT_FOUND", "The review resource was not found.");

    public static readonly DomainError ResourceArchived = new(
        "REVIEW_RESOURCE_ARCHIVED",
        "An archived resource cannot be added to review.");

    public static readonly DomainError InvalidResourceType = new(
        "REVIEW_INVALID_RESOURCE_TYPE", "The review resource type is invalid.");

    public static readonly DomainError InvalidEvaluation = new(
        "REVIEW_INVALID_EVALUATION", "The review evaluation is invalid.");

    public static readonly DomainError ScheduleConflict = new(
        "REVIEW_SCHEDULE_CONFLICT",
        "The review item could not be updated because its schedule changed.");

    public static readonly DomainError SubmissionReused = new(
        "REVIEW_SUBMISSION_REUSED",
        "The review submission identifier was already used for another item.");
}
