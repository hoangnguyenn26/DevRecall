namespace DevRecall.Application.Knowledge.UpdateMetadata;

public sealed record UpdateKnowledgeMetadataResult(
    Guid Id,
    string? Description,
    string? SourceUrl,
    DateTimeOffset UpdatedAtUtc);
