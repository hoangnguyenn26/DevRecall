namespace DevRecall.Contracts.Knowledge;

public sealed record UpdateKnowledgeNodeResponse(
    Guid Id,
    Guid? ParentId,
    string Title,
    DateTimeOffset UpdatedAtUtc);
