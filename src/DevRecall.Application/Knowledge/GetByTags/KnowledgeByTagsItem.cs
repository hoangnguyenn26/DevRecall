namespace DevRecall.Application.Knowledge.GetByTags;

public sealed record KnowledgeByTagsItem(
    Guid Id,
    Guid? ParentId,
    string Title,
    string? Description,
    int SortOrder,
    IReadOnlyList<KnowledgeNodeTagItem> Tags);
