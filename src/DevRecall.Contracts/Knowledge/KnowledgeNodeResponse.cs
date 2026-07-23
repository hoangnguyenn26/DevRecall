namespace DevRecall.Contracts.Knowledge;

public sealed record KnowledgeNodeResponse(
    Guid Id,
    Guid? ParentId,
    string Title,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc);
