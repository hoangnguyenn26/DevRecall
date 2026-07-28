using DevRecall.Application.Reviews.Resources;
using DevRecall.Domain.Reviews;
using DevRecall.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace DevRecall.Infrastructure.Reviews.Resources;

internal sealed class ReviewResourceSummaryReader(DevRecallDbContext dbContext)
    : IReviewResourceSummaryReader
{
    public async Task<IReadOnlyList<ReviewResourceSummary>> ReadManyAsync(
        Guid userId,
        IReadOnlyCollection<ReviewResourceReference> resources,
        CancellationToken cancellationToken)
    {
        if (resources.Count == 0)
        {
            return [];
        }

        var results = new List<ReviewResourceSummary>();
        var knowledgeIds = IdsFor(
            resources, ReviewResourceType.KnowledgeNode);
        var interviewIds = IdsFor(
            resources, ReviewResourceType.InterviewQuestion);
        var dsaIds = IdsFor(resources, ReviewResourceType.DsaProblem);

        if (knowledgeIds.Length > 0)
        {
            var rows = await dbContext.KnowledgeNodes
                .AsNoTracking()
                .Where(node =>
                    node.UserId == userId && knowledgeIds.Contains(node.Id))
                .Select(node => new { node.Id, node.Title, node.Content })
                .ToListAsync(cancellationToken);
            results.AddRange(rows.Select(row => new ReviewResourceSummary(
                ReviewResourceType.KnowledgeNode, row.Id, row.Title,
                ReviewPreviewBuilder.Build(row.Content))));
        }

        if (interviewIds.Length > 0)
        {
            var rows = await dbContext.InterviewQuestions
                .AsNoTracking()
                .Where(question =>
                    question.UserId == userId
                    && interviewIds.Contains(question.Id))
                .Select(question => new
                {
                    question.Id,
                    question.Title,
                    question.Question
                })
                .ToListAsync(cancellationToken);
            results.AddRange(rows.Select(row => new ReviewResourceSummary(
                ReviewResourceType.InterviewQuestion, row.Id, row.Title,
                ReviewPreviewBuilder.Build(row.Question))));
        }

        if (dsaIds.Length > 0)
        {
            var rows = await dbContext.DsaProblems
                .AsNoTracking()
                .Where(problem =>
                    problem.UserId == userId && dsaIds.Contains(problem.Id))
                .Select(problem => new
                {
                    problem.Id,
                    problem.Title,
                    problem.Description
                })
                .ToListAsync(cancellationToken);
            results.AddRange(rows.Select(row => new ReviewResourceSummary(
                ReviewResourceType.DsaProblem, row.Id, row.Title,
                ReviewPreviewBuilder.Build(row.Description))));
        }

        return results;
    }

    private static Guid[] IdsFor(
        IEnumerable<ReviewResourceReference> resources,
        ReviewResourceType resourceType) =>
        resources
            .Where(resource => resource.ResourceType == resourceType)
            .Select(resource => resource.ResourceId)
            .Distinct()
            .ToArray();
}
