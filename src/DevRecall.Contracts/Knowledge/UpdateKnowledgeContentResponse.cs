namespace DevRecall.Contracts.Knowledge;

public sealed record UpdateKnowledgeContentResponse(
    Guid Id,
    string Content,
    DateTimeOffset UpdatedAtUtc);
