using DevRecall.Domain.Knowledge;

namespace DevRecall.Application.Knowledge;

public interface IKnowledgeNodeRepository
{
    Task<KnowledgeNode?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken);

    Task<KnowledgeNode?> GetByIdAndUserIdAsync(
        Guid id,
        Guid userId,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<KnowledgeNode>> GetActiveByUserIdAsync(
        Guid userId,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<KnowledgeNodeHierarchyItem>> GetHierarchyAsync(
        Guid userId,
        CancellationToken cancellationToken);

    Task<int> GetNextSortOrderAsync(
        Guid userId,
        Guid? parentId,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<KnowledgeNode>> GetActiveSiblingsAsync(
        Guid userId,
        Guid? parentId,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<KnowledgeNodeWithTagsItem>> GetActiveByTagIdsAsync(
        Guid userId,
        IReadOnlyCollection<Guid> tagIds,
        CancellationToken cancellationToken);

    void Add(KnowledgeNode node);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
