using DevRecall.Application.Reviews;
using DevRecall.Domain.Reviews;
using DevRecall.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace DevRecall.Infrastructure.Reviews;

internal sealed class ReviewItemRepository(DevRecallDbContext dbContext)
    : IReviewItemRepository
{
    public Task<ReviewItem?> GetByIdAsync(
        Guid id, CancellationToken cancellationToken) =>
        dbContext.ReviewItems.SingleOrDefaultAsync(
            item => item.Id == id, cancellationToken);

    public Task<ReviewItem?> GetByIdAndUserIdAsync(
        Guid id, Guid userId, CancellationToken cancellationToken) =>
        dbContext.ReviewItems
            .AsNoTracking()
            .SingleOrDefaultAsync(
                item => item.Id == id && item.UserId == userId,
                cancellationToken);

    public Task<bool> ActiveExistsAsync(
        Guid userId, ReviewResourceType resourceType, Guid resourceId,
        CancellationToken cancellationToken) =>
        dbContext.ReviewItems
            .AsNoTracking()
            .AnyAsync(
                item => item.UserId == userId
                    && item.ResourceType == resourceType
                    && item.ResourceId == resourceId
                    && item.Status == ReviewItemStatus.Active,
                cancellationToken);

    public void Add(ReviewItem reviewItem) =>
        dbContext.ReviewItems.Add(reviewItem);

    public Task SaveChangesAsync(CancellationToken cancellationToken) =>
        dbContext.SaveChangesAsync(cancellationToken);
}
