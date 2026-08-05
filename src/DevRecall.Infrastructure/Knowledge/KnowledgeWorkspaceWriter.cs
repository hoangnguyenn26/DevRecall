using DevRecall.Application.Common.Exceptions;
using DevRecall.Application.Knowledge;
using DevRecall.Application.Knowledge.Workspace;
using DevRecall.Domain.Knowledge;
using DevRecall.Domain.Knowledge.Tags;
using DevRecall.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace DevRecall.Infrastructure.Knowledge;

internal sealed class KnowledgeWorkspaceWriter(DevRecallDbContext dbContext)
    : IKnowledgeWorkspaceWriter
{
    public async Task<KnowledgeForUpdate?> GetForUpdateAsync(
        Guid userId, Guid knowledgeId, CancellationToken cancellationToken)
    {
        var node = await dbContext.KnowledgeNodes.SingleOrDefaultAsync(item =>
            item.Id == knowledgeId && item.UserId == userId, cancellationToken);
        if (node is null) return null;
        var tagIds = await dbContext.KnowledgeNodeTags.AsNoTracking()
            .Where(relation => relation.KnowledgeNodeId == knowledgeId)
            .Select(relation => relation.TagId).ToListAsync(cancellationToken);
        return new KnowledgeForUpdate(node, tagIds);
    }

    public Task<bool> TopicExistsAsync(Guid userId, Guid topicId, CancellationToken cancellationToken) =>
        dbContext.KnowledgeNodes.AsNoTracking().AnyAsync(node => node.Id == topicId
            && node.UserId == userId && node.Status == KnowledgeNodeStatus.Active, cancellationToken);

    public Task<int> CountAvailableTagsAsync(Guid userId, IReadOnlyCollection<Guid> tagIds,
        CancellationToken cancellationToken) => dbContext.Tags.AsNoTracking().CountAsync(tag =>
            tag.UserId == userId && tag.Status == TagStatus.Active && tagIds.Contains(tag.Id), cancellationToken);

    public async Task<IReadOnlyList<KnowledgeNodeHierarchyItem>> GetHierarchyAsync(
        Guid userId, CancellationToken cancellationToken) => await dbContext.KnowledgeNodes.AsNoTracking()
        .Where(node => node.UserId == userId)
        .Select(node => new KnowledgeNodeHierarchyItem(node.Id, node.ParentId))
        .ToListAsync(cancellationToken);

    public void ReplaceTags(Guid knowledgeId, IReadOnlyCollection<Guid> currentTagIds,
        IReadOnlyCollection<Guid> tagIds, DateTimeOffset currentUtc)
    {
        var removed = currentTagIds.Except(tagIds).ToArray();
        var added = tagIds.Except(currentTagIds).ToArray();
        if (removed.Length > 0)
            dbContext.KnowledgeNodeTags.RemoveRange(dbContext.KnowledgeNodeTags.Where(relation =>
                relation.KnowledgeNodeId == knowledgeId && removed.Contains(relation.TagId)));
        if (added.Length > 0)
            dbContext.KnowledgeNodeTags.AddRange(added.Select(tagId =>
                KnowledgeNodeTag.Create(knowledgeId, tagId, currentUtc)));
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        try { await dbContext.SaveChangesAsync(cancellationToken); }
        catch (DbUpdateConcurrencyException exception)
        {
            throw new ConcurrencyException("KNOWLEDGE_CONFLICT",
                "The knowledge item changed since it was loaded.", exception);
        }
    }
}
