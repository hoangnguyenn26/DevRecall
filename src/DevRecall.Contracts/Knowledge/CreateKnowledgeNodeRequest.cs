namespace DevRecall.Contracts.Knowledge;

public sealed record CreateKnowledgeNodeRequest(string Title, Guid? ParentId);
