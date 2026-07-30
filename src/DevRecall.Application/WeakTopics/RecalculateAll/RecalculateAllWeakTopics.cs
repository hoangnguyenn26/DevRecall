using DevRecall.Application.Common.Exceptions;
using DevRecall.Application.Common.Time;
using DevRecall.Application.Identity;
using DevRecall.Domain.WeakTopics;

namespace DevRecall.Application.WeakTopics.RecalculateAll;

public sealed record RecalculateAllWeakTopicsCommand;
public sealed record WeakTopicCandidate(
    WeakTopicResourceType ResourceType, Guid ResourceId);
public sealed record WeakTopicBatchSignalReadModel(
    WeakTopicResourceType ResourceType, Guid ResourceId,
    WeaknessSignalType SignalType, DateTimeOffset OccurredAtUtc);
public sealed record RecalculateAllWeakTopicsResult(
    DateTimeOffset CalculatedAtUtc, int CandidateResources,
    int CreatedProfiles, int UpdatedProfiles, int UnchangedProfiles,
    int NoneProfiles, int LowProfiles, int MediumProfiles,
    int HighProfiles, int CriticalProfiles);

public interface IWeakTopicCandidateReader
{
    Task<IReadOnlyList<WeakTopicCandidate>> ReadAsync(
        Guid userId, DateTimeOffset fromUtc, DateTimeOffset toUtc,
        CancellationToken cancellationToken);
}

public interface IWeakTopicBatchSignalReader
{
    Task<IReadOnlyList<WeakTopicBatchSignalReadModel>> ReadAsync(
        Guid userId, DateTimeOffset fromUtc, DateTimeOffset toUtc,
        CancellationToken cancellationToken);
}

public sealed class RecalculateAllWeakTopicsHandler(
    IWeakTopicCandidateReader candidateReader,
    IWeakTopicBatchSignalReader signalReader,
    IWeakTopicProfileRepository profileRepository,
    ICurrentUser currentUser, IUtcClock utcClock)
{
    private const int MaximumCandidates = 500;

    public async Task<RecalculateAllWeakTopicsResult> HandleAsync(
        RecalculateAllWeakTopicsCommand command,
        CancellationToken cancellationToken)
    {
        var userId = currentUser.UserId
            ?? throw new UnauthorizedException("AUTH_REQUIRED", "Authentication is required.");
        var calculatedAtUtc = utcClock.UtcNow;
        var fromUtc = calculatedAtUtc.AddDays(-90);
        var candidates = await candidateReader.ReadAsync(
            userId, fromUtc, calculatedAtUtc, cancellationToken);
        if (candidates.Count > MaximumCandidates)
        {
            throw new ConflictException(
                WeakTopicErrors.BatchTooLarge.Code,
                WeakTopicErrors.BatchTooLarge.Message);
        }

        var rows = await signalReader.ReadAsync(
            userId, fromUtc, calculatedAtUtc, cancellationToken);
        var signals = rows.GroupBy(x => (x.ResourceType, x.ResourceId))
            .ToDictionary(x => x.Key, x => x.Select(row =>
                new WeaknessSignal(row.SignalType, row.OccurredAtUtc)).ToArray());
        var profiles = await profileRepository.GetByUserIdForUpdateAsync(
            userId, cancellationToken);
        var byResource = profiles.ToDictionary(
            x => (x.ResourceType, x.ResourceId));
        var created = 0;
        var updated = 0;
        var unchanged = 0;
        foreach (var candidate in candidates)
        {
            var key = (candidate.ResourceType, candidate.ResourceId);
            var breakdown = WeakTopicScoringPolicy.Calculate(
                signals.GetValueOrDefault(key) ?? [], calculatedAtUtc);
            if (!byResource.TryGetValue(key, out var profile))
            {
                profile = WeakTopicProfile.Create(
                    Guid.NewGuid(), userId, candidate.ResourceType,
                    candidate.ResourceId, breakdown, calculatedAtUtc);
                profileRepository.Add(profile);
                byResource[key] = profile;
                created++;
            }
            else if (profile.Recalculate(breakdown, calculatedAtUtc))
            {
                updated++;
            }
            else
            {
                unchanged++;
            }
        }

        if (created > 0 || updated > 0)
        {
            try
            {
                await profileRepository.SaveChangesAsync(cancellationToken);
            }
            catch (WeakTopicProfileConflictException)
            {
                throw new ConflictException(
                    WeakTopicErrors.BatchConflict.Code,
                    WeakTopicErrors.BatchConflict.Message);
            }
        }

        var processed = candidates.Select(x =>
            byResource[(x.ResourceType, x.ResourceId)]).ToArray();
        return new(
            calculatedAtUtc, candidates.Count, created, updated, unchanged,
            processed.Count(x => x.Level == WeaknessLevel.None),
            processed.Count(x => x.Level == WeaknessLevel.Low),
            processed.Count(x => x.Level == WeaknessLevel.Medium),
            processed.Count(x => x.Level == WeaknessLevel.High),
            processed.Count(x => x.Level == WeaknessLevel.Critical));
    }
}
