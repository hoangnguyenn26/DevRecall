using DevRecall.Domain.Common.Errors;

namespace DevRecall.Domain.Study;

public static class StudySessionErrors
{
    public static readonly DomainError SessionNotFound = new(
        "STUDY_SESSION_NOT_FOUND", "The study session was not found.");
    public static readonly DomainError SessionAlreadyStarted = new(
        "STUDY_SESSION_ALREADY_STARTED",
        "The study session has already started.");
    public static readonly DomainError SessionNotStarted = new(
        "STUDY_SESSION_NOT_STARTED", "The study session has not started.");
    public static readonly DomainError SessionCompleted = new(
        "STUDY_SESSION_COMPLETED",
        "A completed study session cannot be changed.");
    public static readonly DomainError SessionNotCompleted = new(
        "STUDY_SESSION_NOT_COMPLETED",
        "Only a completed study session can have a reflection.");
    public static readonly DomainError SessionCancelled = new(
        "STUDY_SESSION_CANCELLED",
        "A cancelled study session cannot be changed.");
    public static readonly DomainError SessionInvalidState = new(
        "STUDY_SESSION_INVALID_STATE",
        "The study session is not in a valid state for this operation.");
    public static readonly DomainError ItemNotFound = new(
        "STUDY_SESSION_ITEM_NOT_FOUND",
        "The study session item was not found.");
    public static readonly DomainError ItemAlreadyExists = new(
        "STUDY_SESSION_ITEM_ALREADY_EXISTS",
        "This resource already exists in the study session.");
    public static readonly DomainError ItemInvalidState = new(
        "STUDY_SESSION_ITEM_INVALID_STATE",
        "The study session item is not in a valid state for this operation.");
    public static readonly DomainError ResourceNotFound = new(
        "STUDY_RESOURCE_NOT_FOUND", "The study resource was not found.");
    public static readonly DomainError ResourceArchived = new(
        "STUDY_RESOURCE_ARCHIVED",
        "An archived resource cannot be added to a study session.");
    public static readonly DomainError InvalidResourceType = new(
        "STUDY_INVALID_RESOURCE_TYPE",
        "The study resource type is invalid.");
    public static readonly DomainError Conflict = new(
        "STUDY_SESSION_CONFLICT",
        "The study session could not be updated because its state changed.");
}
