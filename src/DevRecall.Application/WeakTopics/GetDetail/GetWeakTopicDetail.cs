using DevRecall.Application.Common.Exceptions;
using DevRecall.Application.Identity;
using DevRecall.Application.WeakTopics.Recalculate;
using DevRecall.Application.WeakTopics.Signals;
using DevRecall.Domain.WeakTopics;
using Microsoft.Extensions.Logging;

namespace DevRecall.Application.WeakTopics.GetDetail;

public sealed record GetWeakTopicDetailQuery(Guid ProfileId);
public sealed record WeakTopicDetailReadModel(
    Guid ProfileId, WeakTopicResourceType ResourceType, Guid ResourceId,
    decimal Score, WeaknessLevel Level, int SignalCount, int Version,
    DateTimeOffset CalculatedAtUtc, DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc);
public sealed record WeakTopicSignalGroup(
    string SignalType, int Count, decimal TotalWeightedScore);
public sealed record WeakTopicSignalContributionItem(
    string SignalType, DateTimeOffset OccurredAtUtc, int BaseWeight,
    decimal RecencyMultiplier, decimal WeightedScore);
public sealed record WeakTopicReasonItem(string Type, int Count);
public sealed record GetWeakTopicDetailResult(
    Guid ProfileId, string ResourceType, Guid ResourceId, string ResourceTitle,
    string? ResourcePreview, bool IsResourceAvailable, decimal Score, string Level,
    int SignalCount, int Version, DateTimeOffset CalculatedAtUtc,
    DateTimeOffset CreatedAtUtc, DateTimeOffset UpdatedAtUtc,
    DateTimeOffset SignalWindowFromUtc, DateTimeOffset SignalWindowToUtc,
    DateTimeOffset? LatestSignalAtUtc,
    IReadOnlyList<WeakTopicReasonItem> Reasons,
    IReadOnlyList<WeakTopicSignalGroup> SignalGroups,
    IReadOnlyList<WeakTopicSignalContributionItem> Contributions);

public interface IWeakTopicDetailReader
{
    Task<WeakTopicDetailReadModel?> FindAsync(
        Guid userId, Guid profileId, CancellationToken cancellationToken);
}

public sealed partial class GetWeakTopicDetailHandler(
    IWeakTopicDetailReader detailReader, IWeakTopicResourceReader resourceReader,
    IWeakTopicSignalReader signalReader, ICurrentUser currentUser,
    ILogger<GetWeakTopicDetailHandler> logger)
{
    public async Task<GetWeakTopicDetailResult> HandleAsync(
        GetWeakTopicDetailQuery query, CancellationToken cancellationToken)
    {
        if (query.ProfileId == Guid.Empty)
        {
            throw new ValidationException(new Dictionary<string, string[]>
            {
                ["profileId"] = ["Weak topic profile id is required."]
            });
        }

        var userId = currentUser.UserId
            ?? throw new UnauthorizedException("AUTH_REQUIRED", "Authentication is required.");
        var profile = await detailReader.FindAsync(
            userId, query.ProfileId, cancellationToken);
        if (profile is null)
        {
            throw new NotFoundException(
                WeakTopicErrors.ProfileNotFound.Code,
                WeakTopicErrors.ProfileNotFound.Message);
        }

        var resource = await resourceReader.FindAsync(
            userId, profile.ResourceType, profile.ResourceId, cancellationToken);
        var toUtc = profile.CalculatedAtUtc;
        var fromUtc = toUtc.AddDays(-90);
        var rows = await signalReader.ReadAsync(
            userId, profile.ResourceType, profile.ResourceId, fromUtc, toUtc,
            cancellationToken);
        var breakdown = WeakTopicScoringPolicy.Calculate(
            rows.Select(x => new WeaknessSignal(
                x.SignalType, x.OccurredAtUtc)).ToArray(), toUtc);
        if (breakdown.FinalScore != profile.Score
            || breakdown.Level != profile.Level
            || breakdown.SignalCount != profile.SignalCount)
        {
            LogScoreMismatch(logger, profile.ProfileId);
        }

        var contributions = breakdown.Contributions
            .OrderByDescending(x => x.OccurredAtUtc)
            .ThenBy(x => x.SignalType)
            .Take(10).Select(x =>
            new WeakTopicSignalContributionItem(
                x.SignalType.ToString(), x.OccurredAtUtc, x.BaseWeight,
                x.RecencyMultiplier, x.WeightedScore)).ToArray();
        var groups = breakdown.Contributions.GroupBy(x => x.SignalType)
            .Select(group => new WeakTopicSignalGroup(
                group.Key.ToString(), group.Count(),
                Math.Round(group.Sum(x => x.WeightedScore), 2,
                    MidpointRounding.AwayFromZero)))
            .OrderByDescending(x => x.TotalWeightedScore)
            .ThenBy(x => x.SignalType).ToArray();
        var reasons = groups.Select(group =>
            new WeakTopicReasonItem(group.SignalType, group.Count)).ToArray();
        return new(
            profile.ProfileId, profile.ResourceType.ToString(), profile.ResourceId,
            resource?.Title ?? "Unavailable resource", resource?.Preview,
            resource?.IsAvailable ?? false, profile.Score, profile.Level.ToString(),
            profile.SignalCount, profile.Version, profile.CalculatedAtUtc,
            profile.CreatedAtUtc, profile.UpdatedAtUtc, fromUtc, toUtc,
            contributions.Length == 0
                ? null : contributions[0].OccurredAtUtc,
            reasons, groups, contributions);
    }

    [LoggerMessage(
        EventId = 1405,
        Level = LogLevel.Warning,
        Message = "Weak topic profile {ProfileId} explanation differs from persisted score")]
    private static partial void LogScoreMismatch(ILogger logger, Guid profileId);
}
