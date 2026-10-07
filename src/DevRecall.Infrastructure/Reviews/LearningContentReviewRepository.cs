using DevRecall.Application.Reviews.LearningContent;
using DevRecall.Domain.LearningContent;
using DevRecall.Domain.Reviews;
using DevRecall.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace DevRecall.Infrastructure.Reviews;

internal sealed class LearningContentReviewRepository(DevRecallDbContext dbContext)
    : ILearningContentReviewRepository
{
    public async Task<LearningContentReviewBatchResult?> GetSubmissionAsync(Guid userId, Guid submissionId,
        CancellationToken cancellationToken)
    {
        var submission = await dbContext.LearningContentReviewSubmissions.AsNoTracking()
            .SingleOrDefaultAsync(item => item.UserId == userId && item.SubmissionId == submissionId,
                cancellationToken);
        if (submission is null) return null;
        var items = await dbContext.LearningContentReviewSubmissionItems.AsNoTracking()
            .Where(item => item.UserId == userId && item.SubmissionId == submissionId)
            .OrderBy(item => item.CandidateKey)
            .Select(item => new LearningContentReviewBatchItem(
                item.CandidateKey, item.ReviewItemId, item.WasCreated))
            .ToListAsync(cancellationToken);
        return new(submission.CreatedCount, submission.ExistingCount, items);
    }

    public Task<LearningReviewContentSource?> GetPublishedContentAsync(string slug,
        CancellationToken cancellationToken) => dbContext.LearningContents.AsNoTracking()
        .Where(content => content.Slug == slug && content.Status == ContentStatus.Published
            && content.ContentType == LearningContentType.Lesson)
        .Select(content => new LearningReviewContentSource(content.Id, content.Title,
            content.ReviewCandidates.OrderBy(candidate => candidate.Position)
                .Select(candidate => new LearningReviewCandidateSource(candidate.Id, candidate.Key,
                    candidate.Prompt, candidate.Answer)).ToList()))
        .SingleOrDefaultAsync(cancellationToken);

    public async Task<IReadOnlyList<ExistingLearningReview>> GetActiveReviewsAsync(Guid userId,
        IReadOnlyCollection<Guid> candidateIds, CancellationToken cancellationToken) =>
        await dbContext.ReviewItems.AsNoTracking()
            .Where(item => item.UserId == userId && item.ResourceType == ReviewResourceType.LearningContent
                && item.Status == ReviewItemStatus.Active && candidateIds.Contains(item.ResourceId))
            .Select(item => new ExistingLearningReview(item.ResourceId, item.Id))
            .ToListAsync(cancellationToken);

    public void AddBatch(LearningContentReviewSubmission submission,
        IReadOnlyCollection<LearningContentReviewSubmissionItem> submissionItems,
        IReadOnlyCollection<ReviewItem> reviewItems,
        IReadOnlyCollection<ReviewLearningContentSource> sources)
    {
        dbContext.LearningContentReviewSubmissions.Add(submission);
        dbContext.LearningContentReviewSubmissionItems.AddRange(submissionItems);
        dbContext.ReviewItems.AddRange(reviewItems);
        dbContext.ReviewLearningContentSources.AddRange(sources);
    }

    public async Task<bool> TrySaveChangesAsync(CancellationToken cancellationToken)
    {
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
            return true;
        }
        catch (DbUpdateException exception) when (exception.InnerException is PostgresException
        { SqlState: PostgresErrorCodes.UniqueViolation })
        {
            dbContext.ChangeTracker.Clear();
            return false;
        }
    }
}
