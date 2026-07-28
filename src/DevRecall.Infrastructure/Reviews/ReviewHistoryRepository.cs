using DevRecall.Application.Reviews;
using DevRecall.Domain.Reviews;
using DevRecall.Infrastructure.Persistence;

namespace DevRecall.Infrastructure.Reviews;

internal sealed class ReviewHistoryRepository(DevRecallDbContext dbContext)
    : IReviewHistoryRepository
{
    public void Add(ReviewHistory history) =>
        dbContext.ReviewHistories.Add(history);
}
