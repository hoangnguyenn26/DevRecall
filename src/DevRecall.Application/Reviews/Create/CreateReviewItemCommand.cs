namespace DevRecall.Application.Reviews.Create;

public sealed record CreateReviewItemCommand(string ResourceType, Guid ResourceId);
