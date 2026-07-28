namespace DevRecall.Application.Reviews.Resources;

public interface IReviewKnowledgeResourceReader
{
    Task<ReviewSourceResource?> FindAsync(
        Guid userId, Guid resourceId, CancellationToken cancellationToken);
}
