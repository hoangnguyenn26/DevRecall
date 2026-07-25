namespace DevRecall.Contracts.Knowledge;

public sealed record KnowledgeNodeListItemResponse(
    Guid Id,
    Guid? ParentId,
    string Title,
    string? Description,
    int SortOrder,
    IReadOnlyList<KnowledgeNodeTagResponse> Tags);
