using DevRecall.Domain.Knowledge.Tags;

namespace DevRecall.Application.Knowledge.Tags;

public interface IKnowledgeNodeTagRepository
{
    Task<bool> ExistsAsync(
        Guid knowledgeNodeId,
        Guid tagId,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<KnowledgeNodeTagItem>> GetTagsByKnowledgeNodeIdAsync(
        Guid knowledgeNodeId,
        CancellationToken cancellationToken);

    void Add(KnowledgeNodeTag relation);

    Task<KnowledgeNodeTag?> GetAsync(
        Guid knowledgeNodeId,
        Guid tagId,
        CancellationToken cancellationToken);

    void Remove(KnowledgeNodeTag relation);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
