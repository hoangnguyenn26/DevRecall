namespace DevRecall.Application.Knowledge.GetTree;

public sealed record KnowledgeTreeItem(
    Guid Id,
    Guid? ParentId,
    string Title,
    int SortOrder,
    IReadOnlyList<KnowledgeTreeItem> Children);
