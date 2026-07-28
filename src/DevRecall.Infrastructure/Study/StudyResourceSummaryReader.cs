using DevRecall.Application.Study.Resources;
using DevRecall.Domain.Study;
using DevRecall.Infrastructure.Persistence;
using DevRecall.Infrastructure.Reviews.Resources;
using Microsoft.EntityFrameworkCore;

namespace DevRecall.Infrastructure.Study;

internal sealed class StudyResourceSummaryReader(DevRecallDbContext dbContext)
    : IStudyResourceSummaryReader
{
    public async Task<IReadOnlyList<StudyResourceSummary>> ReadManyAsync(
        Guid userId,
        IReadOnlyCollection<StudyResourceReference> resources,
        CancellationToken cancellationToken)
    {
        var results = new List<StudyResourceSummary>();
        var knowledgeIds = Ids(resources, StudyResourceType.KnowledgeNode);
        var interviewIds = Ids(
            resources, StudyResourceType.InterviewQuestion);
        var dsaIds = Ids(resources, StudyResourceType.DsaProblem);
        var reviewIds = Ids(resources, StudyResourceType.ReviewItem);
        if (knowledgeIds.Length > 0)
        {
            var rows = await dbContext.KnowledgeNodes.AsNoTracking()
                .Where(node =>
                    node.UserId == userId && knowledgeIds.Contains(node.Id))
                .Select(node => new { node.Id, node.Title, node.Content })
                .ToListAsync(cancellationToken);
            results.AddRange(rows.Select(row => new StudyResourceSummary(
                StudyResourceType.KnowledgeNode, row.Id, row.Title,
                ReviewPreviewBuilder.Build(row.Content))));
        }

        if (interviewIds.Length > 0)
        {
            var rows = await dbContext.InterviewQuestions.AsNoTracking()
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
            results.AddRange(rows.Select(row => new StudyResourceSummary(
                StudyResourceType.InterviewQuestion, row.Id, row.Title,
                ReviewPreviewBuilder.Build(row.Question))));
        }

        if (dsaIds.Length > 0)
        {
            var rows = await dbContext.DsaProblems.AsNoTracking()
                .Where(problem =>
                    problem.UserId == userId && dsaIds.Contains(problem.Id))
                .Select(problem => new
                {
                    problem.Id,
                    problem.Title,
                    problem.Description
                })
                .ToListAsync(cancellationToken);
            results.AddRange(rows.Select(row => new StudyResourceSummary(
                StudyResourceType.DsaProblem, row.Id, row.Title,
                ReviewPreviewBuilder.Build(row.Description))));
        }

        if (reviewIds.Length > 0)
        {
            results.AddRange(await dbContext.ReviewItems.AsNoTracking()
                .Where(item =>
                    item.UserId == userId && reviewIds.Contains(item.Id))
                .Select(item => new StudyResourceSummary(
                    StudyResourceType.ReviewItem, item.Id,
                    "Scheduled Review",
                    item.ResourceType.ToString() + " review"))
                .ToListAsync(cancellationToken));
        }

        return results;
    }

    private static Guid[] Ids(
        IEnumerable<StudyResourceReference> resources,
        StudyResourceType type) =>
        resources.Where(resource => resource.ResourceType == type)
            .Select(resource => resource.ResourceId).Distinct().ToArray();
}
