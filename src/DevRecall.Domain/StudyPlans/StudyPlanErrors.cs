using DevRecall.Domain.Common.Errors;

namespace DevRecall.Domain.StudyPlans;

public static class StudyPlanErrors
{
    public static readonly DomainError NotFound = new(
        "STUDY_PLAN_NOT_FOUND", "The study plan was not found.");
    public static readonly DomainError TitleRequired = new(
        "STUDY_PLAN_TITLE_REQUIRED", "The study plan title is required.");
    public static readonly DomainError TitleTooLong = new(
        "STUDY_PLAN_TITLE_TOO_LONG",
        "The study plan title exceeds the supported length.");
    public static readonly DomainError NotDraft = new(
        "STUDY_PLAN_NOT_DRAFT", "Only a draft study plan can be edited.");
    public static readonly DomainError EmptyPlan = new(
        "STUDY_PLAN_EMPTY", "A study plan must contain at least one item.");
    public static readonly DomainError DuplicateResource = new(
        "STUDY_PLAN_DUPLICATE_RESOURCE",
        "The resource is already included in the study plan.");
    public static readonly DomainError ItemLimitReached = new(
        "STUDY_PLAN_ITEM_LIMIT_REACHED",
        "The study plan has reached its item limit.");
    public static readonly DomainError DurationLimitExceeded = new(
        "STUDY_PLAN_DURATION_LIMIT_EXCEEDED",
        "The study plan exceeds its duration limit.");
    public static readonly DomainError InvalidItemDuration = new(
        "STUDY_PLAN_INVALID_ITEM_DURATION",
        "The planned item duration is invalid.");
    public static readonly DomainError InvalidItemOrder = new(
        "STUDY_PLAN_INVALID_ITEM_ORDER",
        "The study plan item order is invalid.");
    public static readonly DomainError ItemNotFound = new(
        "STUDY_PLAN_ITEM_NOT_FOUND", "The study plan item was not found.");
    public static readonly DomainError NotReady = new(
        "STUDY_PLAN_NOT_READY", "The study plan is not ready for conversion.");
    public static readonly DomainError AlreadyConverted = new(
        "STUDY_PLAN_ALREADY_CONVERTED",
        "The study plan has already been converted.");
    public static readonly DomainError Conflict = new(
        "STUDY_PLAN_CONFLICT",
        "The study plan changed before the operation completed.");
    public static readonly DomainError DraftAlreadyExists = new(
        "STUDY_PLAN_DRAFT_ALREADY_EXISTS",
        "A draft study plan already exists.");
    public static readonly DomainError NoEligibleItems = new(
        "STUDY_PLAN_NO_ELIGIBLE_ITEMS",
        "No eligible recommendation fits the requested study plan.");
}

public sealed class StudyPlanDomainException(DomainError error)
    : InvalidOperationException(error.Message)
{
    public DomainError Error { get; } = error;
}
