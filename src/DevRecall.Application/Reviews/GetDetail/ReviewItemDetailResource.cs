namespace DevRecall.Application.Reviews.GetDetail;

public sealed record ReviewItemDetailResource(
    string ResourceType,
    Guid ResourceId,
    string Title,
    string? Preview,
    bool IsAvailable);
