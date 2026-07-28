using DevRecall.Domain.Reviews;

namespace DevRecall.Application.Reviews.Resources;

public sealed record ReviewResourceSummary(
    ReviewResourceType ResourceType,
    Guid ResourceId,
    string Title,
    string? Preview);
