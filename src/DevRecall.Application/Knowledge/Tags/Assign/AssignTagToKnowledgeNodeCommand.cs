namespace DevRecall.Application.Knowledge.Tags.Assign;

public sealed record AssignTagToKnowledgeNodeCommand(
    Guid KnowledgeNodeId,
    Guid TagId);
