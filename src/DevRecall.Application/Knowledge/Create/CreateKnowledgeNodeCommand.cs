namespace DevRecall.Application.Knowledge.Create;

public sealed record CreateKnowledgeNodeCommand(string Title, Guid? ParentId);
