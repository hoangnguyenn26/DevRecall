namespace DevRecall.Application.Reviews.Resources;

public interface IReviewDsaResourceReader
{
    Task<ReviewSourceResource?> FindAsync(
        Guid userId, Guid resourceId, CancellationToken cancellationToken);
}
