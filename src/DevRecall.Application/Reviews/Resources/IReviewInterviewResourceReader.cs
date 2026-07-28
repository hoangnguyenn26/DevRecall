namespace DevRecall.Application.Reviews.Resources;

public interface IReviewInterviewResourceReader
{
    Task<ReviewSourceResource?> FindAsync(
        Guid userId, Guid resourceId, CancellationToken cancellationToken);
}
