namespace DevRecall.Contracts.Knowledge;

public sealed record ChangeKnowledgeNodePositionRequest(
    Guid? TargetParentId,
    int TargetIndex);
