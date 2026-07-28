namespace DevRecall.Application.Reviews.Resources;

public sealed record ReviewSourceResource(
    Guid Id, string Title, string? Preview, bool IsArchived);
