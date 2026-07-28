using DevRecall.Application.Common.Exceptions;
using DevRecall.Application.Common.Pagination;
using DevRecall.Application.Reviews;
using DevRecall.Application.Reviews.GetDue;
using DevRecall.Domain.Reviews;
using DevRecall.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace DevRecall.Infrastructure.Reviews;

internal sealed class ReviewItemRepository(DevRecallDbContext dbContext)
    : IReviewItemRepository
{
    public Task<ReviewItem?> GetByIdAndUserIdForUpdateAsync(
        Guid id, Guid userId, CancellationToken cancellationToken) =>
        dbContext.ReviewItems.SingleOrDefaultAsync(
            item => item.Id == id && item.UserId == userId,
            cancellationToken);

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

    public async Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception)
            when (exception.InnerException is PostgresException
            {
                ConstraintName: "ux_review_items_active_resource"
            })
        {
            throw new ConflictException(
                ReviewErrors.ItemAlreadyExists.Code,
                ReviewErrors.ItemAlreadyExists.Message);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new ConflictException(
                ReviewErrors.ScheduleConflict.Code,
                ReviewErrors.ScheduleConflict.Message);
        }
    }

    public async Task<PagedReadResult<DueReviewItemReadModel>> GetDueAsync(
        Guid userId, DateTimeOffset dueAtOrBeforeUtc,
        ReviewResourceType? resourceType, int skip, int take,
        CancellationToken cancellationToken)
    {
        var query = dbContext.ReviewItems
            .AsNoTracking()
            .Where(item =>
                item.UserId == userId
                && item.Status == ReviewItemStatus.Active
                && item.DueAtUtc <= dueAtOrBeforeUtc);

        if (resourceType is not null)
        {
            query = query.Where(
                item => item.ResourceType == resourceType.Value);
        }

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderBy(item => item.DueAtUtc)
            .ThenBy(item => item.CreatedAtUtc)
            .ThenBy(item => item.Id)
            .Skip(skip)
            .Take(take)
            .Select(item => new DueReviewItemReadModel(
                item.Id, item.ResourceType, item.ResourceId, item.DueAtUtc,
                item.LastReviewedAtUtc, item.IntervalDays, item.ReviewCount))
            .ToListAsync(cancellationToken);
        return new PagedReadResult<DueReviewItemReadModel>(items, totalCount);
    }
}
