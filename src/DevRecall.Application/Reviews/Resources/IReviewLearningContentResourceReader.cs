namespace DevRecall.Application.Reviews.Resources;

public interface IReviewLearningContentResourceReader
{
    Task<ReviewSourceResource?> FindAsync(
        Guid userId, Guid resourceId, CancellationToken cancellationToken);
}
