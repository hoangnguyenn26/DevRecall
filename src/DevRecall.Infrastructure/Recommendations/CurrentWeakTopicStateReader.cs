using DevRecall.Application.Recommendations.GetList;
using DevRecall.Application.Recommendations.Synchronize;
using DevRecall.Domain.Recommendations;
using DevRecall.Domain.WeakTopics;
using DevRecall.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace DevRecall.Infrastructure.Recommendations;

internal sealed class CurrentWeakTopicStateReader(DevRecallDbContext dbContext)
    : ICurrentWeakTopicStateReader
{
    public async Task<IReadOnlyList<CurrentWeakTopicState>> ReadManyAsync(
        Guid userId, IReadOnlyCollection<RecommendationResourceReference> resources,
        CancellationToken cancellationToken)
    {
        var result = new List<CurrentWeakTopicState>();
        await AddAsync(
            RecommendationResourceType.KnowledgeNode,
            WeakTopicResourceType.KnowledgeNode, result);
        await AddAsync(
            RecommendationResourceType.InterviewQuestion,
            WeakTopicResourceType.InterviewQuestion, result);
        await AddAsync(
            RecommendationResourceType.DsaProblem,
            WeakTopicResourceType.DsaProblem, result);
        return result;

        async Task AddAsync(
            RecommendationResourceType recommendationType,
            WeakTopicResourceType weakType,
            List<CurrentWeakTopicState> target)
        {
            var ids = resources.Where(x => x.ResourceType == recommendationType)
                .Select(x => x.ResourceId).Distinct().ToArray();
            if (ids.Length == 0)
            {
                return;
            }

            target.AddRange(await dbContext.WeakTopicProfiles.AsNoTracking()
                .Where(x => x.UserId == userId && x.ResourceType == weakType
                    && ids.Contains(x.ResourceId))
                .Select(x => new CurrentWeakTopicState(
                    x.ResourceType, x.ResourceId, x.Score, x.Level))
                .ToListAsync(cancellationToken));
        }
    }
}
