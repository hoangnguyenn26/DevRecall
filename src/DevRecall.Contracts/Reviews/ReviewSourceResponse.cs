namespace DevRecall.Contracts.Reviews;

public sealed record ReviewSourceResponse(
    string Type,
    string Title,
    string Slug,
    bool IsAvailable);
