namespace DevRecall.Application.Knowledge;

public sealed record KnowledgeNodeWithTagsItem(
    Guid Id,
    Guid? ParentId,
    string Title,
    string? Description,
    int SortOrder,
    IReadOnlyList<KnowledgeNodeTagItem> Tags);
