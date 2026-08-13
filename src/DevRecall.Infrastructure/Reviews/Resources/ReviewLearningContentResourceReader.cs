using DevRecall.Application.Reviews.Resources;
using DevRecall.Domain.Reviews;
using DevRecall.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace DevRecall.Infrastructure.Reviews.Resources;

internal sealed class ReviewLearningContentResourceReader(DevRecallDbContext dbContext)
    : IReviewLearningContentResourceReader
{
    public async Task<ReviewSourceResource?> FindAsync(Guid userId, Guid resourceId,
        CancellationToken cancellationToken)
    {
        var row = await (
            from source in dbContext.ReviewLearningContentSources.AsNoTracking()
            join review in dbContext.ReviewItems.AsNoTracking() on source.ReviewItemId equals review.Id
            where source.UserId == userId && source.CandidateId == resourceId
                && review.Status == ReviewItemStatus.Active
            select new { source.CandidateId, source.PromptSnapshot, source.AnswerSnapshot })
            .SingleOrDefaultAsync(cancellationToken);
        return row is null ? null : new(row.CandidateId, row.PromptSnapshot, row.AnswerSnapshot, false);
    }
}
