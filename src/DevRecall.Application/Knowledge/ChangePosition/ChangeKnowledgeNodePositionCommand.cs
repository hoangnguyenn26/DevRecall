namespace DevRecall.Application.Knowledge.ChangePosition;

public sealed record ChangeKnowledgeNodePositionCommand(
    Guid Id,
    Guid? TargetParentId,
    int TargetIndex);
