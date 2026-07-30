using DevRecall.Domain.Common.Errors;

namespace DevRecall.Domain.Recommendations;

public static class RecommendationErrors
{
    public static readonly DomainError NotFound = new(
        "RECOMMENDATION_NOT_FOUND", "The study recommendation was not found.");
    public static readonly DomainError AlreadyExists = new(
        "RECOMMENDATION_ALREADY_EXISTS",
        "An active recommendation already exists for this resource.");
    public static readonly DomainError NotActive = new(
        "RECOMMENDATION_NOT_ACTIVE",
        "The study recommendation is no longer active.");
    public static readonly DomainError InvalidResourceType = new(
        "RECOMMENDATION_INVALID_RESOURCE_TYPE",
        "The recommendation resource type is invalid.");
    public static readonly DomainError InvalidType = new(
        "RECOMMENDATION_INVALID_TYPE", "The recommendation type is invalid.");
    public static readonly DomainError Conflict = new(
        "RECOMMENDATION_CONFLICT",
        "The recommendation changed before the operation completed.");
    public static readonly DomainError InvalidVersion = new(
        "RECOMMENDATION_INVALID_VERSION",
        "The expected recommendation version is invalid.");
}

public sealed class RecommendationDomainException(DomainError error)
    : InvalidOperationException(error.Message)
{
    public DomainError Error { get; } = error;
}
