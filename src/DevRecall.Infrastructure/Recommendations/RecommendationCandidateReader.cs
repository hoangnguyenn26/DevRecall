using DevRecall.Application.Recommendations.Generation;
using DevRecall.Domain.WeakTopics;
using DevRecall.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace DevRecall.Infrastructure.Recommendations;

internal sealed class RecommendationCandidateReader(DevRecallDbContext dbContext)
    : IRecommendationCandidateReader
{
    public async Task<IReadOnlyList<RecommendationCandidate>> ReadAsync(
        Guid userId, int take, CancellationToken cancellationToken)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(take);
        if (take > 500)
        {
            throw new ArgumentOutOfRangeException(
                nameof(take), "Take cannot exceed 500.");
        }

        return await dbContext.WeakTopicProfiles.AsNoTracking()
            .Where(x => x.UserId == userId
                && x.Level != WeaknessLevel.None && x.Score > 0m)
            .OrderByDescending(x => x.Score)
            .ThenByDescending(x => x.CalculatedAtUtc)
            .ThenBy(x => x.Id)
            .Take(take)
            .Select(x => new RecommendationCandidate(
                x.Id, x.ResourceType, x.ResourceId, x.Score, x.Level,
                x.SignalCount, x.CalculatedAtUtc))
            .ToListAsync(cancellationToken);
    }
}
