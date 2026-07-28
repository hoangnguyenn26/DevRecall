using DevRecall.Domain.Reviews;

namespace DevRecall.Application.Reviews.Resources;

public interface IReviewResourceResolver
{
    Task<ReviewResourceResolution?> ResolveAsync(
        Guid userId, ReviewResourceType resourceType, Guid resourceId,
        CancellationToken cancellationToken);
}
