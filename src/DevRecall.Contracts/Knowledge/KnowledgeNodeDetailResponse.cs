namespace DevRecall.Contracts.Knowledge;

public sealed record KnowledgeNodeDetailResponse(
    Guid Id,
    Guid? ParentId,
    string Title,
    string Content,
    string? Description,
    string? SourceUrl,
    string Status,
    int SortOrder,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc);
