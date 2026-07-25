namespace DevRecall.Contracts.Knowledge;

public sealed record UpdateKnowledgeMetadataRequest(
    string? Description,
    string? SourceUrl);
