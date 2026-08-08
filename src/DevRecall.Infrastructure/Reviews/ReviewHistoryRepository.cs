using DevRecall.Application.Common.Pagination;
using DevRecall.Application.Reviews;
using DevRecall.Application.Reviews.Evaluate;
using DevRecall.Application.Reviews.GetDetail;
using DevRecall.Domain.Reviews;
using DevRecall.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace DevRecall.Infrastructure.Reviews;

internal sealed class ReviewHistoryRepository(DevRecallDbContext dbContext)
    : IReviewHistoryRepository
{
    public void Add(ReviewHistory history) =>
        dbContext.ReviewHistories.Add(history);

    public Task<ReviewSubmissionReadModel?> GetBySubmissionAsync(
        Guid userId, Guid submissionId, CancellationToken cancellationToken) =>
        dbContext.ReviewHistories
            .AsNoTracking()
            .Where(history => history.UserId == userId
                && history.SubmissionId == submissionId)
            .Select(history => new ReviewSubmissionReadModel(
                history.UserId!.Value, history.SubmissionId!.Value,
                history.ReviewItemId, history.Id,
                history.Evaluation.ToString(), history.PreviousIntervalDays,
                history.NextIntervalDays, history.PreviousDueAtUtc,
                history.ReviewedAtUtc, history.NextDueAtUtc,
                history.ResultReviewCount!.Value))
            .SingleOrDefaultAsync(cancellationToken);

    public Task<List<ReviewHistoryReadModel>> GetRecentAsync(
        Guid reviewItemId, int take, CancellationToken cancellationToken) =>
        BaseQuery(reviewItemId)
            .Take(take)
            .ToListAsync(cancellationToken);

    async Task<IReadOnlyList<ReviewHistoryReadModel>>
        IReviewHistoryRepository.GetRecentAsync(
            Guid reviewItemId, int take,
            CancellationToken cancellationToken) =>
        await GetRecentAsync(reviewItemId, take, cancellationToken);

    public async Task<PagedReadResult<ReviewHistoryReadModel>> GetListAsync(
        Guid reviewItemId, int skip, int take,
        CancellationToken cancellationToken)
    {
        var query = dbContext.ReviewHistories
            .AsNoTracking()
            .Where(history => history.ReviewItemId == reviewItemId);
        var totalCount = await query.CountAsync(cancellationToken);
        var items = await BaseQuery(reviewItemId)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken);
        return new PagedReadResult<ReviewHistoryReadModel>(items, totalCount);
    }

    private IQueryable<ReviewHistoryReadModel> BaseQuery(Guid reviewItemId) =>
        dbContext.ReviewHistories
            .AsNoTracking()
            .Where(history => history.ReviewItemId == reviewItemId)
            .OrderByDescending(history => history.ReviewedAtUtc)
            .ThenByDescending(history => history.Id)
            .Select(history => new ReviewHistoryReadModel(
                history.Id, history.Evaluation, history.PreviousIntervalDays,
                history.NextIntervalDays, history.PreviousDueAtUtc,
                history.NextDueAtUtc, history.ReviewedAtUtc,
                history.CreatedAtUtc));
}
