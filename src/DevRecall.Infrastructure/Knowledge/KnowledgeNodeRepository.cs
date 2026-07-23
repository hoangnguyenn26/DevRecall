using DevRecall.Application.Knowledge;
using DevRecall.Domain.Knowledge;
using DevRecall.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace DevRecall.Infrastructure.Knowledge;

internal sealed class KnowledgeNodeRepository(DevRecallDbContext dbContext)
    : IKnowledgeNodeRepository
{
    public Task<KnowledgeNode?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken)
    {
        return dbContext.KnowledgeNodes.SingleOrDefaultAsync(
            node => node.Id == id,
            cancellationToken);
    }

    public void Add(KnowledgeNode node)
    {
        dbContext.KnowledgeNodes.Add(node);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        return dbContext.SaveChangesAsync(cancellationToken);
    }
}
