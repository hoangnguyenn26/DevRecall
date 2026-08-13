using DevRecall.Application.Common.Exceptions;
using DevRecall.Application.LearningContent;
using DevRecall.Domain.LearningContent;
using DevRecall.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using LearningContentAggregate = DevRecall.Domain.LearningContent.LearningContent;

namespace DevRecall.Infrastructure.LearningContent;

internal sealed class LearningContentProgressRepository(DevRecallDbContext dbContext)
    : ILearningContentProgressRepository
{
    public Task<LearningContentAggregate?> GetPublishedContentAsync(string slug, CancellationToken cancellationToken) =>
        dbContext.LearningContents.SingleOrDefaultAsync(x => x.Slug == slug && x.Status == ContentStatus.Published,
            cancellationToken);
    public Task<LearningContentProgress?> GetAsync(Guid userId, Guid contentId, CancellationToken cancellationToken) =>
        dbContext.LearningContentProgresses.SingleOrDefaultAsync(x => x.UserId == userId &&
            x.LearningContentId == contentId, cancellationToken);
    public Task<LearningContentCompletionEvidence?> GetCompletionEvidenceAsync(Guid userId, Guid contentId,
        CancellationToken cancellationToken) => dbContext.LearningContentCompletionEvidence.AsNoTracking()
        .SingleOrDefaultAsync(x => x.UserId == userId && x.LearningContentId == contentId, cancellationToken);
    public void Add(LearningContentProgress progress) => dbContext.LearningContentProgresses.Add(progress);
    public void Add(LearningContentCompletionEvidence evidence) => dbContext.LearningContentCompletionEvidence.Add(evidence);
    public async Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        try { await dbContext.SaveChangesAsync(cancellationToken); }
        catch (DbUpdateConcurrencyException exception)
        {
            dbContext.ChangeTracker.Clear();
            throw new ConcurrencyException("LEARNING_CONTENT_PROGRESS_CONFLICT",
                "The lesson progress changed since it was loaded.", exception);
        }
        catch (DbUpdateException exception) when (exception.InnerException is PostgresException
        {
            ConstraintName: "uq_learning_content_progresses_user_content"
                    or "uq_learning_content_completion_evidence_user_content"
        })
        {
            dbContext.ChangeTracker.Clear();
            throw new LearningContentProgressRaceException(
                "Concurrent learning progress converged on the canonical state.", exception);
        }
    }
}
