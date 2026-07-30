using DevRecall.Application.WeakTopics.Recalculate;
using DevRecall.Domain.Dsa;
using DevRecall.Domain.Interview;
using DevRecall.Domain.Knowledge;
using DevRecall.Domain.WeakTopics;
using DevRecall.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace DevRecall.Infrastructure.WeakTopics;

internal sealed class WeakTopicResourceReader(DevRecallDbContext dbContext)
    : IWeakTopicResourceReader
{
    private const int PreviewLength = 160;

    public Task<WeakTopicResourceReadModel?> FindAsync(
        Guid userId, WeakTopicResourceType resourceType, Guid resourceId,
        CancellationToken cancellationToken) =>
        resourceType switch
        {
            WeakTopicResourceType.KnowledgeNode => dbContext.KnowledgeNodes
                .AsNoTracking().Where(x => x.UserId == userId && x.Id == resourceId)
                .Select(x => new WeakTopicResourceReadModel(
                    resourceType, x.Id, x.Title,
                    x.Content.Length > PreviewLength
                        ? x.Content.Substring(0, PreviewLength) : x.Content,
                    x.Status == KnowledgeNodeStatus.Active))
                .SingleOrDefaultAsync(cancellationToken),
            WeakTopicResourceType.InterviewQuestion => dbContext.InterviewQuestions
                .AsNoTracking().Where(x => x.UserId == userId && x.Id == resourceId)
                .Select(x => new WeakTopicResourceReadModel(
                    resourceType, x.Id, x.Title,
                    x.Question.Length > PreviewLength
                        ? x.Question.Substring(0, PreviewLength) : x.Question,
                    x.Status == InterviewQuestionStatus.Active))
                .SingleOrDefaultAsync(cancellationToken),
            WeakTopicResourceType.DsaProblem => dbContext.DsaProblems
                .AsNoTracking().Where(x => x.UserId == userId && x.Id == resourceId)
                .Select(x => new WeakTopicResourceReadModel(
                    resourceType, x.Id, x.Title,
                    x.Description.Length > PreviewLength
                        ? x.Description.Substring(0, PreviewLength) : x.Description,
                    x.Status == DsaProblemStatus.Active))
                .SingleOrDefaultAsync(cancellationToken),
            _ => Task.FromResult<WeakTopicResourceReadModel?>(null)
        };
}
