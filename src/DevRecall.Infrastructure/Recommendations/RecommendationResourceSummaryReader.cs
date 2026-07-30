using DevRecall.Application.Recommendations.GetList;
using DevRecall.Domain.Dsa;
using DevRecall.Domain.Interview;
using DevRecall.Domain.Knowledge;
using DevRecall.Domain.Recommendations;
using DevRecall.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace DevRecall.Infrastructure.Recommendations;

internal sealed class RecommendationResourceSummaryReader(
    DevRecallDbContext dbContext) : IRecommendationResourceSummaryReader
{
    private const int PreviewLength = 160;

    public async Task<IReadOnlyList<RecommendationResourceSummary>> ReadManyAsync(
        Guid userId, IReadOnlyCollection<RecommendationResourceReference> resources,
        CancellationToken cancellationToken)
    {
        var result = new List<RecommendationResourceSummary>();
        var knowledgeIds = Ids(resources, RecommendationResourceType.KnowledgeNode);
        if (knowledgeIds.Length > 0)
        {
            result.AddRange(await dbContext.KnowledgeNodes.AsNoTracking()
                .Where(x => x.UserId == userId && knowledgeIds.Contains(x.Id))
                .Select(x => new RecommendationResourceSummary(
                    RecommendationResourceType.KnowledgeNode, x.Id, x.Title,
                    x.Content.Length > PreviewLength
                        ? x.Content.Substring(0, PreviewLength) : x.Content,
                    x.Status == KnowledgeNodeStatus.Active))
                .ToListAsync(cancellationToken));
        }

        var interviewIds = Ids(
            resources, RecommendationResourceType.InterviewQuestion);
        if (interviewIds.Length > 0)
        {
            result.AddRange(await dbContext.InterviewQuestions.AsNoTracking()
                .Where(x => x.UserId == userId && interviewIds.Contains(x.Id))
                .Select(x => new RecommendationResourceSummary(
                    RecommendationResourceType.InterviewQuestion, x.Id, x.Title,
                    x.Question.Length > PreviewLength
                        ? x.Question.Substring(0, PreviewLength) : x.Question,
                    x.Status == InterviewQuestionStatus.Active))
                .ToListAsync(cancellationToken));
        }

        var dsaIds = Ids(resources, RecommendationResourceType.DsaProblem);
        if (dsaIds.Length > 0)
        {
            result.AddRange(await dbContext.DsaProblems.AsNoTracking()
                .Where(x => x.UserId == userId && dsaIds.Contains(x.Id))
                .Select(x => new RecommendationResourceSummary(
                    RecommendationResourceType.DsaProblem, x.Id, x.Title,
                    x.Description.Length > PreviewLength
                        ? x.Description.Substring(0, PreviewLength) : x.Description,
                    x.Status == DsaProblemStatus.Active))
                .ToListAsync(cancellationToken));
        }

        return result;
    }

    private static Guid[] Ids(
        IEnumerable<RecommendationResourceReference> resources,
        RecommendationResourceType type) =>
        resources.Where(x => x.ResourceType == type)
            .Select(x => x.ResourceId).Distinct().ToArray();
}
