namespace DevRecall.Application.Knowledge.Update;

public sealed record UpdateKnowledgeNodeResult(
    Guid Id,
    Guid? ParentId,
    string Title,
    DateTimeOffset UpdatedAtUtc);
