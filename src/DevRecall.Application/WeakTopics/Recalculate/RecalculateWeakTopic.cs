using DevRecall.Application.Common.Exceptions;
using DevRecall.Application.Common.Time;
using DevRecall.Application.Identity;
using DevRecall.Application.WeakTopics.Signals;
using DevRecall.Domain.WeakTopics;

namespace DevRecall.Application.WeakTopics.Recalculate;

public sealed record RecalculateWeakTopicCommand(string ResourceType, Guid ResourceId);
public sealed record WeakTopicResourceReadModel(
    WeakTopicResourceType ResourceType, Guid ResourceId, string Title,
    string? Preview, bool IsAvailable);
public sealed record WeakTopicContributionItem(
    string SignalType, int Weight, decimal RecencyMultiplier, decimal WeightedScore);
public sealed record RecalculateWeakTopicResult(
    Guid ProfileId, string ResourceType, Guid ResourceId, string ResourceTitle,
    bool IsResourceAvailable, decimal Score, string Level, int SignalCount,
    int Version, DateTimeOffset CalculatedAtUtc, bool WasCreated,
    IReadOnlyList<WeakTopicContributionItem> Contributions);

public interface IWeakTopicResourceReader
{
    Task<WeakTopicResourceReadModel?> FindAsync(
        Guid userId, WeakTopicResourceType resourceType, Guid resourceId,
        CancellationToken cancellationToken);
}

public sealed class RecalculateWeakTopicHandler(
    IWeakTopicProfileRepository repository, IWeakTopicSignalReader signalReader,
    IWeakTopicResourceReader resourceReader, ICurrentUser currentUser, IUtcClock clock)
{
    public async Task<RecalculateWeakTopicResult> HandleAsync(
        RecalculateWeakTopicCommand command, CancellationToken cancellationToken)
    {
        if (command.ResourceId == Guid.Empty)
        {
            throw WeakTopicResourceTypeParser.Invalid("resourceId", "Resource id is required.");
        }

        var type = WeakTopicResourceTypeParser.Parse(command.ResourceType);
        var userId = currentUser.UserId
            ?? throw new UnauthorizedException("AUTH_REQUIRED", "Authentication is required.");
        var resource = await resourceReader.FindAsync(
            userId, type, command.ResourceId, cancellationToken);
        if (resource is null)
        {
            throw new NotFoundException(
                WeakTopicErrors.ResourceNotFound.Code, WeakTopicErrors.ResourceNotFound.Message);
        }

        var now = clock.UtcNow;
        var signals = await signalReader.ReadAsync(
            userId, type, command.ResourceId, now.AddDays(-90), now, cancellationToken);
        var score = WeakTopicScoringPolicy.Calculate(
            signals.Select(x => new WeaknessSignal(x.SignalType, x.OccurredAtUtc)).ToArray(),
            now);
        var profile = await repository.GetByUserAndResourceForUpdateAsync(
            userId, type, command.ResourceId, cancellationToken);
        var wasCreated = profile is null;
        var changed = true;
        if (profile is null)
        {
            profile = WeakTopicProfile.Create(
                Guid.NewGuid(), userId, type, command.ResourceId, score, now);
            repository.Add(profile);
        }
        else
        {
            changed = profile.Recalculate(score, now);
        }

        if (changed)
        {
            try
            {
                await repository.SaveChangesAsync(cancellationToken);
            }
            catch (WeakTopicProfileConflictException)
            {
                throw new ConflictException(
                    WeakTopicErrors.CalculationConflict.Code,
                    WeakTopicErrors.CalculationConflict.Message);
            }
        }

        return new RecalculateWeakTopicResult(
            profile.Id, type.ToString(), command.ResourceId, resource.Title,
            resource.IsAvailable, profile.Score, profile.Level.ToString(),
            profile.SignalCount, profile.Version, profile.CalculatedAtUtc, wasCreated,
            score.Contributions.Select(x => new WeakTopicContributionItem(
                x.SignalType.ToString(), x.BaseWeight, x.RecencyMultiplier,
                x.WeightedScore)).ToArray());
    }
}
