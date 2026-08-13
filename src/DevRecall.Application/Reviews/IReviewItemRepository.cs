using DevRecall.Application.Common.Pagination;
using DevRecall.Application.Reviews.GetDue;
using DevRecall.Domain.Reviews;

namespace DevRecall.Application.Reviews;

public interface IReviewItemRepository
{
    Task<ReviewItem?> GetByIdAndUserIdForUpdateAsync(
        Guid id, Guid userId, CancellationToken cancellationToken);

    Task<ReviewItem?> GetByIdAndUserIdAsync(
        Guid id, Guid userId, CancellationToken cancellationToken);

    Task<bool> ActiveExistsAsync(
        Guid userId, ReviewResourceType resourceType, Guid resourceId,
        CancellationToken cancellationToken);

    void Add(ReviewItem reviewItem);
    Task SaveChangesAsync(CancellationToken cancellationToken);

    Task<PagedReadResult<DueReviewItemReadModel>> GetDueAsync(
        Guid userId, DateTimeOffset dueAtOrBeforeUtc,
        ReviewResourceType? resourceType, int skip, int take,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<ReviewSourceProvenance>> GetLearningContentSourcesAsync(
        Guid userId, IReadOnlyCollection<Guid> reviewItemIds,
        CancellationToken cancellationToken);
}
