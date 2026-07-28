using DevRecall.Application.Reviews.Resources;
using DevRecall.Domain.Reviews;

namespace DevRecall.Infrastructure.Reviews;

internal sealed class ReviewResourceResolver(
    IReviewKnowledgeResourceReader knowledgeReader,
    IReviewInterviewResourceReader interviewReader,
    IReviewDsaResourceReader dsaReader)
    : IReviewResourceResolver
{
    public async Task<ReviewResourceResolution?> ResolveAsync(
        Guid userId, ReviewResourceType resourceType, Guid resourceId,
        CancellationToken cancellationToken)
    {
        var resource = resourceType switch
        {
            ReviewResourceType.KnowledgeNode =>
                await knowledgeReader.FindAsync(
                    userId, resourceId, cancellationToken),
            ReviewResourceType.InterviewQuestion =>
                await interviewReader.FindAsync(
                    userId, resourceId, cancellationToken),
            ReviewResourceType.DsaProblem =>
                await dsaReader.FindAsync(
                    userId, resourceId, cancellationToken),
            _ => null
        };

        return resource is null
            ? null
            : new ReviewResourceResolution(
                resourceType, resource.Id, resource.Title, resource.Preview,
                resource.IsArchived
                    ? ReviewResourceAvailability.Archived
                    : ReviewResourceAvailability.Available);
    }
}
