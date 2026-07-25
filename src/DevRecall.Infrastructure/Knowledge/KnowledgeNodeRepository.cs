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

    public Task<KnowledgeNode?> GetByIdAndUserIdAsync(
        Guid id,
        Guid userId,
        CancellationToken cancellationToken)
    {
        return dbContext.KnowledgeNodes
            .AsNoTracking()
            .SingleOrDefaultAsync(
                node => node.Id == id && node.UserId == userId,
                cancellationToken);
    }

    public async Task<IReadOnlyList<KnowledgeNode>> GetActiveByUserIdAsync(
        Guid userId,
        CancellationToken cancellationToken)
    {
        return await dbContext.KnowledgeNodes
            .AsNoTracking()
            .Where(node =>
                node.UserId == userId
                && node.Status == KnowledgeNodeStatus.Active)
            .OrderBy(node => node.SortOrder)
            .ThenBy(node => node.Title)
            .ThenBy(node => node.Id)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<KnowledgeNodeHierarchyItem>>
        GetHierarchyAsync(
            Guid userId,
            CancellationToken cancellationToken)
    {
        return await dbContext.KnowledgeNodes
            .AsNoTracking()
            .Where(node => node.UserId == userId)
            .Select(node => new KnowledgeNodeHierarchyItem(
                node.Id,
                node.ParentId))
            .ToListAsync(cancellationToken);
    }

    public void Add(KnowledgeNode node)
    {
        dbContext.KnowledgeNodes.Add(node);
    }

    public async Task<int> GetNextSortOrderAsync(
        Guid userId,
        Guid? parentId,
        CancellationToken cancellationToken)
    {
        var maximum = await dbContext.KnowledgeNodes
            .AsNoTracking()
            .Where(node =>
                node.UserId == userId
                && node.ParentId == parentId
                && node.Status == KnowledgeNodeStatus.Active)
            .Select(node => (int?)node.SortOrder)
            .MaxAsync(cancellationToken);

        return maximum is null ? 0 : maximum.Value + 1;
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        return dbContext.SaveChangesAsync(cancellationToken);
    }
}
