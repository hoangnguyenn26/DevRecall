namespace DevRecall.Contracts.Reviews;

public sealed record ReviewResourceSummaryResponse(
    string ResourceType,
    Guid ResourceId,
    string Title,
    string? Preview,
    bool IsAvailable);
