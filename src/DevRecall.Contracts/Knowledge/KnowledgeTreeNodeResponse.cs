namespace DevRecall.Contracts.Knowledge;

public sealed record KnowledgeTreeNodeResponse(
    Guid Id,
    Guid? ParentId,
    string Title,
    int SortOrder,
    IReadOnlyList<KnowledgeTreeNodeResponse> Children);
