using DevRecall.Domain.Reviews;

namespace DevRecall.Application.Reviews;

public interface IReviewItemRepository
{
    Task<ReviewItem?> GetByIdAsync(
        Guid id, CancellationToken cancellationToken);

    Task<ReviewItem?> GetByIdAndUserIdAsync(
        Guid id, Guid userId, CancellationToken cancellationToken);

    Task<bool> ActiveExistsAsync(
        Guid userId, ReviewResourceType resourceType, Guid resourceId,
        CancellationToken cancellationToken);

    void Add(ReviewItem reviewItem);
    Task SaveChangesAsync(CancellationToken cancellationToken);
}
