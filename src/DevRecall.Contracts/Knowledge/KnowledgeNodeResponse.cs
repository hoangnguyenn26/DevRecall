namespace DevRecall.Contracts.Knowledge;

public sealed record KnowledgeNodeResponse(
    Guid Id,
    Guid? ParentId,
    string Title,
    int SortOrder,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc);
