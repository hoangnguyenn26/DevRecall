using DevRecall.Application.Common.Exceptions;
using DevRecall.Application.Common.Time;
using DevRecall.Application.Identity;
using DevRecall.Domain.Recommendations;

namespace DevRecall.Application.Recommendations.Generation;

public sealed record GenerateRecommendationsCommand(int MaximumCandidates);
public sealed record GenerateRecommendationsResult(
    DateTimeOffset GeneratedAtUtc, int CandidateCount,
    int CreatedCount, int UpdatedCount, int UnchangedCount,
    int LowPriorityCount, int MediumPriorityCount,
    int HighPriorityCount, int CriticalPriorityCount);

public sealed class GenerateRecommendationsHandler(
    IRecommendationCandidateReader candidateReader,
    IStudyRecommendationRepository repository,
    ICurrentUser currentUser, IUtcClock utcClock)
{
    private const int MaximumSupportedCandidates = 500;

    public async Task<GenerateRecommendationsResult> HandleAsync(
        GenerateRecommendationsCommand command,
        CancellationToken cancellationToken)
    {
        if (command.MaximumCandidates is < 1 or > MaximumSupportedCandidates)
        {
            throw new ValidationException(new Dictionary<string, string[]>
            {
                ["maximumCandidates"] =
                [$"MaximumCandidates must be between 1 and {MaximumSupportedCandidates}."]
            });
        }

        var userId = currentUser.UserId
            ?? throw new UnauthorizedException("AUTH_REQUIRED", "Authentication is required.");
        var generatedAtUtc = utcClock.UtcNow;
        var candidates = await candidateReader.ReadAsync(
            userId, command.MaximumCandidates, cancellationToken);
        if (candidates.Count == 0)
        {
            return new(
                generatedAtUtc, 0, 0, 0, 0, 0, 0, 0, 0);
        }

        var active = await repository.GetActiveByUserIdForUpdateAsync(
            userId, cancellationToken);
        var byKey = active.ToDictionary(x =>
            new ActiveRecommendationKey(x.ResourceType, x.ResourceId, x.Type));
        var expiresAtUtc = generatedAtUtc.Add(
            RecommendationDefaults.ActiveLifetime);
        var created = 0;
        var updated = 0;
        var unchanged = 0;
        var processed = new List<StudyRecommendation>(candidates.Count);
        foreach (var candidate in candidates)
        {
            var resourceType = RecommendationMappingPolicy.MapResourceType(
                candidate.ResourceType);
            var type = RecommendationMappingPolicy.MapType(candidate.ResourceType);
            var priority = RecommendationPriorityPolicy.FromWeaknessLevel(
                candidate.WeaknessLevel);
            var reason = RecommendationReason.Create(
                candidate.WeaknessScore, candidate.WeaknessLevel,
                candidate.SignalCount, candidate.WeaknessCalculatedAtUtc);
            var key = new ActiveRecommendationKey(
                resourceType, candidate.ResourceId, type);
            if (!byKey.TryGetValue(key, out var recommendation))
            {
                recommendation = StudyRecommendation.Create(
                    Guid.NewGuid(), userId, resourceType, candidate.ResourceId,
                    type, priority, reason, generatedAtUtc, expiresAtUtc);
                repository.Add(recommendation);
                byKey[key] = recommendation;
                created++;
            }
            else if (recommendation.Refresh(
                priority, reason, generatedAtUtc, expiresAtUtc))
            {
                updated++;
            }
            else
            {
                unchanged++;
            }

            processed.Add(recommendation);
        }

        if (created > 0 || updated > 0)
        {
            await SaveAsync(cancellationToken);
        }

        return new(
            generatedAtUtc, candidates.Count, created, updated, unchanged,
            processed.Count(x => x.Priority == RecommendationPriority.Low),
            processed.Count(x => x.Priority == RecommendationPriority.Medium),
            processed.Count(x => x.Priority == RecommendationPriority.High),
            processed.Count(x => x.Priority == RecommendationPriority.Critical));
    }

    private async Task SaveAsync(CancellationToken cancellationToken)
    {
        try
        {
            await repository.SaveChangesAsync(cancellationToken);
        }
        catch (ActiveRecommendationAlreadyExistsException)
        {
            throw new ConflictException(
                RecommendationErrors.AlreadyExists.Code,
                RecommendationErrors.AlreadyExists.Message);
        }
        catch (RecommendationPersistenceConflictException)
        {
            throw new ConflictException(
                RecommendationErrors.Conflict.Code,
                RecommendationErrors.Conflict.Message);
        }
    }

    private readonly record struct ActiveRecommendationKey(
        RecommendationResourceType ResourceType, Guid ResourceId,
        RecommendationType Type);
}
