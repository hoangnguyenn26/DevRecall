using DevRecall.Application.StudyPlans.Resources;
using DevRecall.Domain.Dsa;
using DevRecall.Domain.Interview;
using DevRecall.Domain.Knowledge;
using DevRecall.Domain.LearningContent;
using DevRecall.Domain.StudyPlans;
using DevRecall.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace DevRecall.Infrastructure.StudyPlans;

internal sealed class StudyPlanResourceSummaryReader(
    DevRecallDbContext dbContext) : IStudyPlanResourceSummaryReader
{
    private const int PreviewLength = 160;

    public async Task<IReadOnlyList<StudyPlanResourceSummary>> ReadManyAsync(
        Guid userId, IReadOnlyCollection<StudyPlanResourceReference> resources,
        CancellationToken cancellationToken)
    {
        var result = new List<StudyPlanResourceSummary>();
        var knowledgeIds = Ids(resources, StudyPlanResourceType.KnowledgeNode);
        if (knowledgeIds.Length > 0)
        {
            result.AddRange(await dbContext.KnowledgeNodes.AsNoTracking()
                .Where(x => x.UserId == userId && knowledgeIds.Contains(x.Id))
                .Select(x => new StudyPlanResourceSummary(
                    StudyPlanResourceType.KnowledgeNode, x.Id, x.Title,
                    x.Content.Length > PreviewLength
                        ? x.Content.Substring(0, PreviewLength) : x.Content,
                    x.Status == KnowledgeNodeStatus.Active))
                .ToListAsync(cancellationToken));
        }

        var interviewIds = Ids(
            resources, StudyPlanResourceType.InterviewQuestion);
        if (interviewIds.Length > 0)
        {
            result.AddRange(await dbContext.InterviewQuestions.AsNoTracking()
                .Where(x => x.UserId == userId && interviewIds.Contains(x.Id))
                .Select(x => new StudyPlanResourceSummary(
                    StudyPlanResourceType.InterviewQuestion, x.Id, x.Title,
                    x.Question.Length > PreviewLength
                        ? x.Question.Substring(0, PreviewLength) : x.Question,
                    x.Status == InterviewQuestionStatus.Active))
                .ToListAsync(cancellationToken));
        }

        var dsaIds = Ids(resources, StudyPlanResourceType.DsaProblem);
        if (dsaIds.Length > 0)
        {
            result.AddRange(await dbContext.DsaProblems.AsNoTracking()
                .Where(x => x.UserId == userId && dsaIds.Contains(x.Id))
                .Select(x => new StudyPlanResourceSummary(
                    StudyPlanResourceType.DsaProblem, x.Id, x.Title,
                    x.Description.Length > PreviewLength
                        ? x.Description.Substring(0, PreviewLength)
                        : x.Description,
                    x.Status == DsaProblemStatus.Active))
                .ToListAsync(cancellationToken));
        }

        var lessonIds = Ids(resources, StudyPlanResourceType.LearningContent);
        if (lessonIds.Length > 0)
        {
            result.AddRange(await dbContext.LearningContents.AsNoTracking()
                .Where(x => lessonIds.Contains(x.Id))
                .Select(x => new StudyPlanResourceSummary(StudyPlanResourceType.LearningContent,
                    x.Id, x.Title, x.Summary, x.Status == ContentStatus.Published, x.Slug))
                .ToListAsync(cancellationToken));
        }

        return result;
    }

    private static Guid[] Ids(
        IEnumerable<StudyPlanResourceReference> resources,
        StudyPlanResourceType type) =>
        resources.Where(x => x.ResourceType == type)
            .Select(x => x.ResourceId).Distinct().ToArray();
}
