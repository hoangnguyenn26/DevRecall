namespace DevRecall.Contracts.Reviews;

public sealed record CreateReviewItemRequest(string ResourceType, Guid ResourceId);
