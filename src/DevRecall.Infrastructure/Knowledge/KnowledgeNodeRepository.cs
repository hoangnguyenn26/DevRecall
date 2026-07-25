using DevRecall.Application.Knowledge;
using DevRecall.Domain.Knowledge;
using DevRecall.Domain.Knowledge.Tags;
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

    public async Task<IReadOnlyList<KnowledgeNode>> GetActiveSiblingsAsync(
        Guid userId,
        Guid? parentId,
        CancellationToken cancellationToken)
    {
        return await dbContext.KnowledgeNodes
            .Where(node =>
                node.UserId == userId
                && node.ParentId == parentId
                && node.Status == KnowledgeNodeStatus.Active)
            .OrderBy(node => node.SortOrder)
            .ThenBy(node => node.Title)
            .ThenBy(node => node.Id)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<KnowledgeNodeWithTagsItem>>
        GetActiveByTagIdsAsync(
            Guid userId,
            IReadOnlyCollection<Guid> tagIds,
            CancellationToken cancellationToken)
    {
        var requiredTagCount = tagIds.Count;
        var matchingNodeIds = dbContext.KnowledgeNodeTags
            .AsNoTracking()
            .Where(relation => tagIds.Contains(relation.TagId))
            .GroupBy(relation => relation.KnowledgeNodeId)
            .Where(group => group.Select(relation => relation.TagId)
                .Distinct()
                .Count() == requiredTagCount)
            .Select(group => group.Key);

        var nodes = await dbContext.KnowledgeNodes
            .AsNoTracking()
            .Where(node => node.UserId == userId
                && node.Status == KnowledgeNodeStatus.Active
                && matchingNodeIds.Contains(node.Id))
            .OrderBy(node => node.Title)
            .ThenBy(node => node.Id)
            .Select(node => new
            {
                node.Id,
                node.ParentId,
                node.Title,
                node.Description,
                node.SortOrder
            })
            .ToListAsync(cancellationToken);

        if (nodes.Count == 0)
        {
            return [];
        }

        var nodeIds = nodes.Select(node => node.Id).ToArray();
        var tagRows = await (
            from relation in dbContext.KnowledgeNodeTags.AsNoTracking()
            join tag in dbContext.Tags.AsNoTracking()
                on relation.TagId equals tag.Id
            where nodeIds.Contains(relation.KnowledgeNodeId)
                && tag.Status == TagStatus.Active
            orderby tag.Name, tag.Id
            select new
            {
                relation.KnowledgeNodeId,
                Tag = new KnowledgeNodeTagItem(tag.Id, tag.Name)
            })
            .ToListAsync(cancellationToken);

        var tagsByNodeId = tagRows
            .GroupBy(row => row.KnowledgeNodeId)
            .ToDictionary(
                group => group.Key,
                group => (IReadOnlyList<KnowledgeNodeTagItem>)
                    group.Select(row => row.Tag).ToList());

        return nodes.Select(node => new KnowledgeNodeWithTagsItem(
                node.Id,
                node.ParentId,
                node.Title,
                node.Description,
                node.SortOrder,
                tagsByNodeId.TryGetValue(node.Id, out var tags) ? tags : []))
            .ToList();
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        return dbContext.SaveChangesAsync(cancellationToken);
    }
}
