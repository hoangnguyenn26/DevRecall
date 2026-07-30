using DevRecall.Application.Common.Exceptions;
using DevRecall.Application.Common.Time;
using DevRecall.Application.Identity;
using DevRecall.Application.Recommendations.GetList;
using DevRecall.Domain.Recommendations;
using DevRecall.Domain.WeakTopics;

namespace DevRecall.Application.Recommendations.Synchronize;

public sealed record CurrentWeakTopicState(
    WeakTopicResourceType ResourceType, Guid ResourceId,
    decimal Score, WeaknessLevel Level);
public interface ICurrentWeakTopicStateReader
{
    Task<IReadOnlyList<CurrentWeakTopicState>> ReadManyAsync(
        Guid userId, IReadOnlyCollection<RecommendationResourceReference> resources,
        CancellationToken cancellationToken);
}

public sealed record SynchronizeRecommendationsCommand;
public sealed record SynchronizeRecommendationsResult(
    DateTimeOffset SynchronizedAtUtc, int ActiveRecommendationsChecked,
    int ExpiredRecommendations, int LifetimeElapsedCount,
    int WeaknessResolvedCount, int ResourceUnavailableCount,
    int UnchangedRecommendations);

public sealed class SynchronizeRecommendationsHandler(
    IStudyRecommendationRepository repository,
    ICurrentWeakTopicStateReader weakTopicStateReader,
    IRecommendationResourceSummaryReader resourceSummaryReader,
    ICurrentUser currentUser, IUtcClock utcClock)
{
    public async Task<SynchronizeRecommendationsResult> HandleAsync(
        SynchronizeRecommendationsCommand command,
        CancellationToken cancellationToken)
    {
        var userId = currentUser.UserId
            ?? throw new UnauthorizedException("AUTH_REQUIRED", "Authentication is required.");
        var now = utcClock.UtcNow;
        var active = await repository.GetActiveByUserIdForUpdateAsync(
            userId, cancellationToken);
        if (active.Count == 0)
        {
            return new(now, 0, 0, 0, 0, 0, 0);
        }

        var references = active.Select(x =>
            new RecommendationResourceReference(x.ResourceType, x.ResourceId))
            .Distinct().ToArray();
        var weakStates = await weakTopicStateReader.ReadManyAsync(
            userId, references, cancellationToken);
        var resources = await resourceSummaryReader.ReadManyAsync(
            userId, references, cancellationToken);
        var weakByKey = weakStates.ToDictionary(
            x => (RecommendationMappingPolicy.MapResourceType(x.ResourceType),
                x.ResourceId));
        var resourceByKey = resources.ToDictionary(
            x => (x.ResourceType, x.ResourceId));
        var lifetime = 0;
        var resolved = 0;
        var unavailable = 0;
        var unchanged = 0;
        foreach (var item in active)
        {
            var key = (item.ResourceType, item.ResourceId);
            weakByKey.TryGetValue(key, out var weak);
            resourceByKey.TryGetValue(key, out var resource);
            var reason = DetermineReason(item, weak, resource, now);
            if (reason is null)
            {
                unchanged++;
                continue;
            }

            item.Expire(item.Version, reason.Value, now);
            switch (reason.Value)
            {
                case RecommendationExpirationReason.LifetimeElapsed:
                    lifetime++;
                    break;
                case RecommendationExpirationReason.WeaknessResolved:
                    resolved++;
                    break;
                case RecommendationExpirationReason.ResourceUnavailable:
                    unavailable++;
                    break;
                default:
                    throw new InvalidOperationException(
                        "Unsupported recommendation expiration reason.");
            }
        }

        var expired = lifetime + resolved + unavailable;
        if (expired > 0)
        {
            try
            {
                await repository.SaveChangesAsync(cancellationToken);
            }
            catch (RecommendationPersistenceConflictException)
            {
                throw new ConflictException(
                    RecommendationErrors.Conflict.Code,
                    RecommendationErrors.Conflict.Message);
            }
        }

        return new(
            now, active.Count, expired, lifetime, resolved, unavailable, unchanged);
    }

    private static RecommendationExpirationReason? DetermineReason(
        StudyRecommendation item, CurrentWeakTopicState? weak,
        RecommendationResourceSummary? resource, DateTimeOffset now)
    {
        if (resource is null || !resource.IsAvailable)
        {
            return RecommendationExpirationReason.ResourceUnavailable;
        }

        if (weak is null || weak.Level == WeaknessLevel.None || weak.Score <= 0m)
        {
            return RecommendationExpirationReason.WeaknessResolved;
        }

        return item.ExpiresAtUtc is not null && now >= item.ExpiresAtUtc
            ? RecommendationExpirationReason.LifetimeElapsed : null;
    }
}
