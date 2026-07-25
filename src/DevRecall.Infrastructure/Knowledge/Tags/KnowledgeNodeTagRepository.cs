using DevRecall.Application.Knowledge;
using DevRecall.Application.Knowledge.Tags;
using DevRecall.Domain.Knowledge.Tags;
using DevRecall.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace DevRecall.Infrastructure.Knowledge.Tags;

internal sealed class KnowledgeNodeTagRepository(DevRecallDbContext dbContext)
    : IKnowledgeNodeTagRepository
{
    public Task<bool> ExistsAsync(
        Guid knowledgeNodeId,
        Guid tagId,
        CancellationToken cancellationToken) =>
        dbContext.KnowledgeNodeTags.AnyAsync(
            relation => relation.KnowledgeNodeId == knowledgeNodeId
                && relation.TagId == tagId,
            cancellationToken);

    public async Task<IReadOnlyList<KnowledgeNodeTagItem>>
        GetTagsByKnowledgeNodeIdAsync(
            Guid knowledgeNodeId,
            CancellationToken cancellationToken)
    {
        return await (
            from relation in dbContext.KnowledgeNodeTags.AsNoTracking()
            join tag in dbContext.Tags.AsNoTracking()
                on relation.TagId equals tag.Id
            where relation.KnowledgeNodeId == knowledgeNodeId
                && tag.Status == TagStatus.Active
            orderby tag.Name, tag.Id
            select new KnowledgeNodeTagItem(tag.Id, tag.Name))
            .ToListAsync(cancellationToken);
    }

    public void Add(KnowledgeNodeTag relation) =>
        dbContext.KnowledgeNodeTags.Add(relation);

    public Task<KnowledgeNodeTag?> GetAsync(
        Guid knowledgeNodeId,
        Guid tagId,
        CancellationToken cancellationToken) =>
        dbContext.KnowledgeNodeTags.SingleOrDefaultAsync(
            relation => relation.KnowledgeNodeId == knowledgeNodeId
                && relation.TagId == tagId,
            cancellationToken);

    public void Remove(KnowledgeNodeTag relation) =>
        dbContext.KnowledgeNodeTags.Remove(relation);

    public Task SaveChangesAsync(CancellationToken cancellationToken) =>
        dbContext.SaveChangesAsync(cancellationToken);
}
