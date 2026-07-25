namespace DevRecall.Application.Knowledge.Tags.Remove;

public sealed record RemoveTagFromKnowledgeNodeCommand(
    Guid KnowledgeNodeId,
    Guid TagId);
