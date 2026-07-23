namespace DevRecall.Application.Knowledge.Move;

public sealed record MoveKnowledgeNodeCommand(Guid Id, Guid? ParentId);
