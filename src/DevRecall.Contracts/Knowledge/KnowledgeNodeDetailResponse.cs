namespace DevRecall.Contracts.Knowledge;

public sealed record KnowledgeNodeDetailResponse(
    Guid Id,
    Guid? ParentId,
    string Title,
    string Content,
    string? Description,
    string? SourceUrl,
    string Status,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc);
