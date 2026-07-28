using DevRecall.Application.Common.Pagination;
using DevRecall.Application.Reviews.GetDetail;
using DevRecall.Domain.Reviews;

namespace DevRecall.Application.Reviews;

public interface IReviewHistoryRepository
{
    void Add(ReviewHistory history);

    Task<IReadOnlyList<ReviewHistoryReadModel>> GetRecentAsync(
        Guid reviewItemId, int take, CancellationToken cancellationToken);

    Task<PagedReadResult<ReviewHistoryReadModel>> GetListAsync(
        Guid reviewItemId, int skip, int take,
        CancellationToken cancellationToken);
}
