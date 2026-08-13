namespace DevRecall.Application.Reviews;

public sealed record ReviewSourceProvenance(
    Guid ReviewItemId,
    string Type,
    string Title,
    string Slug,
    bool IsAvailable);
