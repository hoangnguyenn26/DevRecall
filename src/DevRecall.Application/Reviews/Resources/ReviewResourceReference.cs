using DevRecall.Domain.Reviews;

namespace DevRecall.Application.Reviews.Resources;

public sealed record ReviewResourceReference(
    ReviewResourceType ResourceType, Guid ResourceId);
