using DevRecall.Application.Analytics.Insights;
using DevRecall.Domain.Recommendations;
using DevRecall.Domain.WeakTopics;
using DevRecall.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace DevRecall.Infrastructure.Analytics;

internal sealed class LearningInsightSourceReader(DevRecallDbContext db) : ILearningInsightSourceReader
{
    public async Task<IReadOnlyList<LearningInsightSource>> ReadAsync(Guid userId, int take, DateTimeOffset now, CancellationToken cancellationToken) =>
        await db.StudyRecommendations.AsNoTracking()
            .Where(x => x.UserId == userId && x.Status == RecommendationStatus.Active && (x.ExpiresAtUtc == null || x.ExpiresAtUtc > now)
                && (x.Reason.WeaknessLevel == WeaknessLevel.Critical || x.Reason.WeaknessLevel == WeaknessLevel.High))
            .OrderByDescending(x => x.Priority).ThenByDescending(x => x.PriorityScore)
            .ThenByDescending(x => x.GeneratedAtUtc).ThenBy(x => x.Id).Take(take)
            .Select(x => new LearningInsightSource(x.Id, x.ResourceType, x.ResourceId, x.Type, x.Priority,
                x.Reason.WeaknessLevel, x.Reason.SignalCount, x.Reason.WeaknessCalculatedAtUtc, x.GeneratedAtUtc))
            .ToArrayAsync(cancellationToken);
}
