namespace DevRecall.Application.Knowledge.UpdateContent;

public sealed record UpdateKnowledgeContentCommand(
    Guid Id,
    string? Content,
    DateTimeOffset ExpectedUpdatedAtUtc);
