namespace DevRecall.Application.Knowledge.Create;

public sealed record CreateKnowledgeNodeResult(
    Guid Id,
    Guid? ParentId,
    string Title,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc);
