namespace DevRecall.Application.Knowledge.GetByTags;

public sealed record GetKnowledgeByTagsQuery(
    IReadOnlyCollection<Guid> TagIds);
