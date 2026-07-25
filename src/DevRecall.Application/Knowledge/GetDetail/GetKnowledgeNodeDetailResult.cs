namespace DevRecall.Application.Knowledge.GetDetail;

public sealed record GetKnowledgeNodeDetailResult(
    Guid Id,
    Guid? ParentId,
    string Title,
    string Content,
    string? Description,
    string? SourceUrl,
    string Status,
    int SortOrder,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc,
    IReadOnlyList<KnowledgeNodeTagItem> Tags);
