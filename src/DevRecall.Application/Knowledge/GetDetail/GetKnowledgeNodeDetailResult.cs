namespace DevRecall.Application.Knowledge.GetDetail;

public sealed record GetKnowledgeNodeDetailResult(
    Guid Id,
    Guid? ParentId,
    string Title,
    string Content,
    string? Description,
    string? SourceUrl,
    string Status,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc);
