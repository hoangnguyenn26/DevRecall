using DevRecall.Application.StudyPlans.Generation;
using DevRecall.Domain.Recommendations;
using DevRecall.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace DevRecall.Infrastructure.StudyPlans;

internal sealed class StudyPlanRecommendationCandidateReader(
    DevRecallDbContext dbContext) : IStudyPlanRecommendationCandidateReader
{
    private const int MaximumTake = 100;

    public async Task<IReadOnlyList<StudyPlanRecommendationCandidate>> ReadAsync(
        Guid userId, int take, DateTimeOffset currentUtc,
        CancellationToken cancellationToken)
    {
        if (userId == Guid.Empty)
        {
            throw new ArgumentException("User id cannot be empty.", nameof(userId));
        }

        if (take is < 1 or > MaximumTake)
        {
            throw new ArgumentOutOfRangeException(nameof(take));
        }

        if (currentUtc.Offset != TimeSpan.Zero)
        {
            throw new ArgumentException("Timestamp must be UTC.", nameof(currentUtc));
        }

        return await dbContext.StudyRecommendations.AsNoTracking()
            .Where(item => item.UserId == userId
                && item.Status == RecommendationStatus.Active
                && (item.ExpiresAtUtc == null
                    || item.ExpiresAtUtc > currentUtc))
            .OrderByDescending(item => item.Priority)
            .ThenByDescending(item => item.PriorityScore)
            .ThenByDescending(item => item.GeneratedAtUtc)
            .ThenBy(item => item.Id)
            .Take(take)
            .Select(item => new StudyPlanRecommendationCandidate(
                item.Id, item.ResourceType, item.ResourceId, item.Type,
                item.Priority, item.PriorityScore, item.GeneratedAtUtc,
                item.ExpiresAtUtc, item.Version))
            .ToListAsync(cancellationToken);
    }
}
