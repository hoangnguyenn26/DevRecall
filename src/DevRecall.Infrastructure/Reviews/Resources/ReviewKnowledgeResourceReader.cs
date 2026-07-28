using DevRecall.Application.Reviews.Resources;
using DevRecall.Domain.Knowledge;
using DevRecall.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace DevRecall.Infrastructure.Reviews.Resources;

internal sealed class ReviewKnowledgeResourceReader(DevRecallDbContext dbContext)
    : IReviewKnowledgeResourceReader
{
    public async Task<ReviewSourceResource?> FindAsync(
        Guid userId, Guid resourceId, CancellationToken cancellationToken)
    {
        var row = await dbContext.KnowledgeNodes
            .AsNoTracking()
            .Where(node => node.Id == resourceId && node.UserId == userId)
            .Select(node => new
            {
                node.Id,
                node.Title,
                node.Content,
                node.Status
            })
            .SingleOrDefaultAsync(cancellationToken);

        return row is null
            ? null
            : new ReviewSourceResource(
                row.Id, row.Title, ReviewPreviewBuilder.Build(row.Content),
                row.Status == KnowledgeNodeStatus.Archived);
    }
}
