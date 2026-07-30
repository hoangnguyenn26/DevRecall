using DevRecall.Domain.Common.Errors;

namespace DevRecall.Domain.WeakTopics;

public static class WeakTopicErrors
{
    public static readonly DomainError ProfileNotFound = new(
        "WEAK_TOPIC_PROFILE_NOT_FOUND",
        "The weak topic profile was not found.");
    public static readonly DomainError InvalidResourceType = new(
        "WEAK_TOPIC_INVALID_RESOURCE_TYPE",
        "The weak topic resource type is invalid.");
    public static readonly DomainError ResourceNotFound = new(
        "WEAK_TOPIC_RESOURCE_NOT_FOUND",
        "The weak topic resource was not found.");
    public static readonly DomainError CalculationConflict = new(
        "WEAK_TOPIC_CALCULATION_CONFLICT",
        "The weak topic profile could not be recalculated because its state changed.");
    public static readonly DomainError BatchTooLarge = new(
        "WEAK_TOPIC_BATCH_TOO_LARGE",
        "The weak-topic recalculation batch exceeds the supported limit.");
    public static readonly DomainError BatchConflict = new(
        "WEAK_TOPIC_BATCH_CONFLICT",
        "One or more weak-topic profiles changed during recalculation.");
}
