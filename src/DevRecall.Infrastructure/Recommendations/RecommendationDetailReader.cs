using DevRecall.Application.Recommendations.GetDetail;
using DevRecall.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace DevRecall.Infrastructure.Recommendations;

internal sealed class RecommendationDetailReader(DevRecallDbContext dbContext)
    : IRecommendationDetailReader
{
    public Task<RecommendationDetailReadModel?> FindAsync(
        Guid userId, Guid recommendationId, CancellationToken cancellationToken) =>
        dbContext.StudyRecommendations.AsNoTracking()
            .Where(x => x.Id == recommendationId && x.UserId == userId)
            .Select(x => new RecommendationDetailReadModel(
                x.Id, x.ResourceType, x.ResourceId, x.Type, x.Priority,
                x.PriorityScore, x.Status, x.Reason.WeaknessScore,
                x.Reason.WeaknessLevel, x.Reason.SignalCount,
                x.Reason.WeaknessCalculatedAtUtc, x.GeneratedAtUtc,
                x.ExpiresAtUtc, x.DismissedAtUtc, x.CompletedAtUtc,
                x.ExpiredAtUtc, x.CreatedAtUtc, x.UpdatedAtUtc, x.Version))
            .SingleOrDefaultAsync(cancellationToken);
}
