using DevRecall.Application.Knowledge.Workspace;
using DevRecall.Domain.Knowledge;
using DevRecall.Domain.Knowledge.Tags;
using DevRecall.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace DevRecall.Infrastructure.Knowledge;

internal sealed class KnowledgeTagReader(DevRecallDbContext dbContext) : IKnowledgeTagReader
{
    public async Task<IReadOnlyList<KnowledgeTagSummaryReadModel>> SearchAsync(
        Guid userId, string? query, int take, CancellationToken cancellationToken)
    {
        var tags = dbContext.Tags.AsNoTracking().Where(tag =>
            tag.UserId == userId && tag.Status == TagStatus.Active);
        if (query is not null)
        {
            var normalized = query.ToUpperInvariant();
            tags = tags.Where(tag => tag.NormalizedName.Contains(normalized));
        }
        return await tags.OrderBy(tag => tag.Name).ThenBy(tag => tag.Id).Take(take)
            .Select(tag => new KnowledgeTagSummaryReadModel(tag.Id, tag.Name, tag.NormalizedName,
                (from relation in dbContext.KnowledgeNodeTags
                 join node in dbContext.KnowledgeNodes on relation.KnowledgeNodeId equals node.Id
                 where relation.TagId == tag.Id && node.UserId == userId
                     && node.Status == KnowledgeNodeStatus.Active
                 select relation.KnowledgeNodeId).Distinct().Count()))
            .ToListAsync(cancellationToken);
    }
}
