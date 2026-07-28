namespace DevRecall.Application.Reviews.Resources;

public interface IReviewResourceSummaryReader
{
    Task<IReadOnlyList<ReviewResourceSummary>> ReadManyAsync(
        Guid userId,
        IReadOnlyCollection<ReviewResourceReference> resources,
        CancellationToken cancellationToken);
}
