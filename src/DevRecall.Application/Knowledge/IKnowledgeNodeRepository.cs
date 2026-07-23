using DevRecall.Domain.Knowledge;

namespace DevRecall.Application.Knowledge;

public interface IKnowledgeNodeRepository
{
    Task<KnowledgeNode?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken);

    void Add(KnowledgeNode node);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
