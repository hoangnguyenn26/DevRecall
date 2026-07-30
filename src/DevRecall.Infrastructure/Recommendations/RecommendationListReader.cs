using DevRecall.Application.Common.Pagination;
using DevRecall.Application.Recommendations.GetList;
using DevRecall.Domain.Recommendations;
using DevRecall.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace DevRecall.Infrastructure.Recommendations;

internal sealed class RecommendationListReader(DevRecallDbContext dbContext)
    : IRecommendationListReader
{
    public async Task<PagedReadResult<RecommendationListReadModel>> ReadAsync(
        Guid userId, RecommendationStatus status,
        RecommendationPriority? priority,
        RecommendationResourceType? resourceType, RecommendationType? type,
        decimal? minimumPriorityScore, int skip, int take,
        CancellationToken cancellationToken)
    {
        var query = dbContext.StudyRecommendations.AsNoTracking()
            .Where(x => x.UserId == userId && x.Status == status);
        if (priority is not null)
        {
            query = query.Where(x => x.Priority == priority.Value);
        }

        if (resourceType is not null)
        {
            query = query.Where(x => x.ResourceType == resourceType.Value);
        }

        if (type is not null)
        {
            query = query.Where(x => x.Type == type.Value);
        }

        if (minimumPriorityScore is not null)
        {
            query = query.Where(x => x.PriorityScore >= minimumPriorityScore.Value);
        }

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await ApplyOrdering(query, status).Skip(skip).Take(take)
            .Select(x => new RecommendationListReadModel(
                x.Id, x.ResourceType, x.ResourceId, x.Type, x.Priority,
                x.PriorityScore, x.Status, x.Reason.WeaknessScore,
                x.Reason.WeaknessLevel, x.Reason.SignalCount,
                x.Reason.WeaknessCalculatedAtUtc, x.GeneratedAtUtc,
                x.ExpiresAtUtc, x.DismissedAtUtc, x.CompletedAtUtc,
                x.ExpiredAtUtc, x.Version))
            .ToListAsync(cancellationToken);
        return new(items, totalCount);
    }

    private static IQueryable<StudyRecommendation> ApplyOrdering(
        IQueryable<StudyRecommendation> query, RecommendationStatus status) =>
        status switch
        {
            RecommendationStatus.Active => query
                .OrderByDescending(x => x.Priority)
                .ThenByDescending(x => x.PriorityScore)
                .ThenByDescending(x => x.GeneratedAtUtc).ThenBy(x => x.Id),
            RecommendationStatus.Dismissed => query
                .OrderByDescending(x => x.DismissedAtUtc).ThenBy(x => x.Id),
            RecommendationStatus.Completed => query
                .OrderByDescending(x => x.CompletedAtUtc).ThenBy(x => x.Id),
            RecommendationStatus.Expired => query
                .OrderByDescending(x => x.ExpiredAtUtc).ThenBy(x => x.Id),
            _ => throw new ArgumentOutOfRangeException(nameof(status))
        };
}
