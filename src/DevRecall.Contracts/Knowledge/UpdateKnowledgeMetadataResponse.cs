namespace DevRecall.Contracts.Knowledge;

public sealed record UpdateKnowledgeMetadataResponse(
    Guid Id,
    string? Description,
    string? SourceUrl,
    DateTimeOffset UpdatedAtUtc);
