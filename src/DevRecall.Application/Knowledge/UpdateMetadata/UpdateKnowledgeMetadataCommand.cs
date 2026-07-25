namespace DevRecall.Application.Knowledge.UpdateMetadata;

public sealed record UpdateKnowledgeMetadataCommand(
    Guid Id,
    string? Description,
    string? SourceUrl);
