using DevRecall.Application.Reviews.Resources;
using DevRecall.Application.Study.Resources;
using DevRecall.Domain.Reviews;
using DevRecall.Domain.Study;
using DevRecall.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace DevRecall.Infrastructure.Study;

internal sealed class StudyResourceResolver(
    IReviewKnowledgeResourceReader knowledgeReader,
    IReviewInterviewResourceReader interviewReader,
    IReviewDsaResourceReader dsaReader,
    IStudyReviewItemResourceReader reviewItemReader)
    : IStudyResourceResolver
{
    public async Task<StudyResourceResolution?> ResolveAsync(
        Guid userId, StudyResourceType resourceType, Guid resourceId,
        CancellationToken cancellationToken)
    {
        if (resourceType == StudyResourceType.ReviewItem)
        {
            return await reviewItemReader.FindAsync(
                userId, resourceId, cancellationToken);
        }

        ReviewSourceResource? source = resourceType switch
        {
            StudyResourceType.KnowledgeNode =>
                await knowledgeReader.FindAsync(
                    userId, resourceId, cancellationToken),
            StudyResourceType.InterviewQuestion =>
                await interviewReader.FindAsync(
                    userId, resourceId, cancellationToken),
            StudyResourceType.DsaProblem =>
                await dsaReader.FindAsync(
                    userId, resourceId, cancellationToken),
            _ => null
        };
        return source is null
            ? null
            : new StudyResourceResolution(
                resourceType, source.Id, source.Title, source.Preview,
                source.IsArchived
                    ? StudyResourceAvailability.Archived
                    : StudyResourceAvailability.Available);
    }
}

internal sealed class StudyReviewItemResourceReader(
    DevRecallDbContext dbContext)
    : IStudyReviewItemResourceReader
{
    public async Task<StudyResourceResolution?> FindAsync(
        Guid userId, Guid resourceId, CancellationToken cancellationToken)
    {
        var item = await dbContext.ReviewItems
            .AsNoTracking()
            .Where(candidate =>
                candidate.Id == resourceId && candidate.UserId == userId)
            .Select(candidate => new
            {
                candidate.Id,
                candidate.ResourceType,
                candidate.Status
            })
            .SingleOrDefaultAsync(cancellationToken);
        return item is null
            ? null
            : new StudyResourceResolution(
                StudyResourceType.ReviewItem, item.Id,
                $"Review: {item.ResourceType}", null,
                item.Status == ReviewItemStatus.Archived
                    ? StudyResourceAvailability.Archived
                    : StudyResourceAvailability.Available);
    }
}
