namespace DevRecall.Application.Knowledge.UpdateContent;

public sealed record UpdateKnowledgeContentResult(
    Guid Id,
    string Content,
    DateTimeOffset UpdatedAtUtc);
