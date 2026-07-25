namespace DevRecall.Contracts.Knowledge;

public sealed class GetKnowledgeByTagsRequest
{
    public Guid[]? TagIds { get; init; }
}
