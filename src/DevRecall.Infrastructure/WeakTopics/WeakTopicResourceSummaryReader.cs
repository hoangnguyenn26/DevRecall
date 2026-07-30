using DevRecall.Application.WeakTopics.GetList;
using DevRecall.Domain.Dsa;
using DevRecall.Domain.Interview;
using DevRecall.Domain.Knowledge;
using DevRecall.Domain.WeakTopics;
using DevRecall.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace DevRecall.Infrastructure.WeakTopics;

internal sealed class WeakTopicResourceSummaryReader(DevRecallDbContext dbContext)
    : IWeakTopicResourceSummaryReader
{
    private const int PreviewLength = 160;

    public async Task<IReadOnlyList<WeakTopicResourceSummary>> ReadManyAsync(
        Guid userId, IReadOnlyCollection<WeakTopicResourceReference> resources,
        CancellationToken cancellationToken)
    {
        var result = new List<WeakTopicResourceSummary>();
        var knowledgeIds = Ids(resources, WeakTopicResourceType.KnowledgeNode);
        if (knowledgeIds.Length > 0)
        {
            result.AddRange(await dbContext.KnowledgeNodes.AsNoTracking()
                .Where(x => x.UserId == userId && knowledgeIds.Contains(x.Id))
                .Select(x => new WeakTopicResourceSummary(
                    WeakTopicResourceType.KnowledgeNode, x.Id, x.Title,
                    x.Content.Length > PreviewLength
                        ? x.Content.Substring(0, PreviewLength) : x.Content,
                    x.Status == KnowledgeNodeStatus.Active))
                .ToListAsync(cancellationToken));
        }

        var interviewIds = Ids(resources, WeakTopicResourceType.InterviewQuestion);
        if (interviewIds.Length > 0)
        {
            result.AddRange(await dbContext.InterviewQuestions.AsNoTracking()
                .Where(x => x.UserId == userId && interviewIds.Contains(x.Id))
                .Select(x => new WeakTopicResourceSummary(
                    WeakTopicResourceType.InterviewQuestion, x.Id, x.Title,
                    x.Question.Length > PreviewLength
                        ? x.Question.Substring(0, PreviewLength) : x.Question,
                    x.Status == InterviewQuestionStatus.Active))
                .ToListAsync(cancellationToken));
        }

        var dsaIds = Ids(resources, WeakTopicResourceType.DsaProblem);
        if (dsaIds.Length > 0)
        {
            result.AddRange(await dbContext.DsaProblems.AsNoTracking()
                .Where(x => x.UserId == userId && dsaIds.Contains(x.Id))
                .Select(x => new WeakTopicResourceSummary(
                    WeakTopicResourceType.DsaProblem, x.Id, x.Title,
                    x.Description.Length > PreviewLength
                        ? x.Description.Substring(0, PreviewLength) : x.Description,
                    x.Status == DsaProblemStatus.Active))
                .ToListAsync(cancellationToken));
        }

        return result;
    }

    private static Guid[] Ids(
        IEnumerable<WeakTopicResourceReference> resources,
        WeakTopicResourceType type) =>
        resources.Where(x => x.ResourceType == type)
            .Select(x => x.ResourceId).Distinct().ToArray();
}
