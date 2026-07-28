using DevRecall.Domain.Reviews;

namespace DevRecall.Application.Reviews.Resources;

public sealed record ReviewResourceResolution(
    ReviewResourceType ResourceType,
    Guid ResourceId,
    string Title,
    string? Preview,
    ReviewResourceAvailability Availability);
