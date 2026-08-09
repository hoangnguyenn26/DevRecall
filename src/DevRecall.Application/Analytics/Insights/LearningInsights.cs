using DevRecall.Application.Common.Exceptions;
using DevRecall.Application.Common.Time;
using DevRecall.Application.Identity;
using DevRecall.Application.Recommendations.GetList;
using DevRecall.Domain.Recommendations;
using DevRecall.Domain.WeakTopics;

namespace DevRecall.Application.Analytics.Insights;

public sealed record LearningInsightSource(Guid RecommendationId, RecommendationResourceType ResourceType,
    Guid ResourceId, RecommendationType ActionType, RecommendationPriority Priority, WeaknessLevel WeaknessLevel,
    int SignalCount, DateTimeOffset WeaknessCalculatedAtUtc, DateTimeOffset GeneratedAtUtc);
public sealed record LearningInsightSignal(string Type, string Label, string Value);
public sealed record LearningInsightAction(string Type, string Label, string TargetType, Guid? TargetId, bool IsAvailable);
public sealed record LearningInsightItem(string Type, string Tone, string Priority, string Title, string Summary,
    IReadOnlyList<LearningInsightSignal> Signals, LearningInsightAction? Action);
public sealed record LearningInsightsResult(DateTimeOffset GeneratedAtUtc, string Range, IReadOnlyList<LearningInsightItem> Items);

public interface ILearningInsightSourceReader
{
    Task<IReadOnlyList<LearningInsightSource>> ReadAsync(Guid userId, int take, DateTimeOffset now, CancellationToken cancellationToken);
}

public static class LearningInsightPolicy
{
    public const int MinimumTrendSampleSize = 3;
    public static readonly TimeSpan MaximumWeakTopicAge = TimeSpan.FromDays(14);
    public static bool HasEnoughEvidence(RatingDistribution current, RatingDistribution previous) => current.Total >= MinimumTrendSampleSize && previous.Total >= MinimumTrendSampleSize;
}

public sealed class GetLearningInsightsHandler(ILearningInsightSourceReader sourceReader,
    IRecommendationResourceSummaryReader resourceReader, IAnalyticsInsightsReader analyticsReader,
    ICurrentUser currentUser, IUtcClock clock)
{
    public async Task<LearningInsightsResult> HandleAsync(string? rangeValue, int take, CancellationToken cancellationToken)
    {
        if (take is < 1 or > 20) throw new ValidationException(new Dictionary<string, string[]> { ["take"] = ["Take must be between 1 and 20."] });
        var (range, current, previous) = ResolveRange(rangeValue);
        var userId = currentUser.UserId ?? throw new UnauthorizedException("AUTH_REQUIRED", "Authentication is required.");
        var sources = await sourceReader.ReadAsync(userId, take, clock.UtcNow, cancellationToken);
        var summaries = await resourceReader.ReadManyAsync(userId, sources.Select(x => new RecommendationResourceReference(x.ResourceType, x.ResourceId)).Distinct().ToArray(), cancellationToken);
        var resources = summaries.ToDictionary(x => (x.ResourceType, x.ResourceId));
        var items = new List<LearningInsightItem>();
        foreach (var source in sources.Where(x => clock.UtcNow - x.WeaknessCalculatedAtUtc < LearningInsightPolicy.MaximumWeakTopicAge))
        {
            resources.TryGetValue((source.ResourceType, source.ResourceId), out var resource);
            items.Add(new("WeakTopicNeedsAttention", "Attention", source.Priority.ToString(),
                $"{resource?.Title ?? "A learning resource"} needs focused practice",
                "Recurring evidence has produced an active recommendation.",
                [new("WeakTopicLevel", "Weak Topic", source.WeaknessLevel.ToString()), new("ContributingSignals", "Learning signals", source.SignalCount.ToString(System.Globalization.CultureInfo.InvariantCulture))],
                new(source.ActionType.ToString(), ActionLabel(source.ActionType), source.ResourceType.ToString(), source.ResourceId, resource?.IsAvailable ?? false)));
        }

        var currentPerformance = await analyticsReader.ReadPerformanceAsync(userId, current, cancellationToken);
        var previousPerformance = await analyticsReader.ReadPerformanceAsync(userId, previous, cancellationToken);
        AddTrend(items, "InterviewPracticeTrend", "Interview self-ratings changed this period", currentPerformance.Interview, previousPerformance.Interview, "Interview", "Open Interview", "Interview", null);
        AddTrend(items, "ReviewRecallTrend", "Review recall outcomes changed this period", currentPerformance.Review, previousPerformance.Review, "Review", "Open Review", "Review", null);
        AddTrend(items, "DsaPracticeTrend", "DSA attempt outcomes changed this period", currentPerformance.Dsa, previousPerformance.Dsa, "Dsa", "Open DSA", "Dsa", null);
        return new(clock.UtcNow, range, items.Take(take).ToArray());
    }

    private static void AddTrend(List<LearningInsightItem> items, string type, string title, RatingDistribution current,
        RatingDistribution previous, string targetType, string label, string actionType, Guid? id)
    {
        if (!LearningInsightPolicy.HasEnoughEvidence(current, previous)) return;
        var currentPositive = current.Third + current.Fourth; var previousPositive = previous.Third + previous.Fourth;
        var currentRatio = decimal.Round(currentPositive * 100m / current.Total, 0); var previousRatio = decimal.Round(previousPositive * 100m / previous.Total, 0);
        if (currentRatio == previousRatio) return;
        var semantics = targetType == "Interview" ? "Good/Strong self-ratings" : targetType == "Review" ? "Good/Easy recall outcomes" : "Solved/Partial attempt outcomes";
        items.Add(new(type, currentRatio > previousRatio ? "Progress" : "Attention", "Medium", title,
            $"The share of {semantics} was {(currentRatio > previousRatio ? "higher" : "lower")} than in the previous period.",
            [new("CurrentRatio", "Current period", $"{currentPositive} of {current.Total} ({currentRatio}%)"), new("PreviousRatio", "Previous period", $"{previousPositive} of {previous.Total} ({previousRatio}%)")],
            new(actionType, label, targetType, id, true)));
    }

    private (string Name, AnalyticsDateRange Current, AnalyticsDateRange Previous) ResolveRange(string? value)
    {
        var name = value ?? "7d"; var days = name switch { "7d" => 7, "30d" => 30, "90d" => 90, _ => throw new ValidationException(new Dictionary<string, string[]> { ["range"] = ["Range must be 7d, 30d, or 90d."] }) };
        var end = new DateTimeOffset(clock.UtcNow.UtcDateTime.Date.AddDays(1), TimeSpan.Zero); var start = end.AddDays(-days);
        return (name, new(start, end), new(start.AddDays(-days), start));
    }

    private static string ActionLabel(RecommendationType type) => type switch { RecommendationType.PracticeInterview => "Practice interview question", RecommendationType.RetryDsaProblem => "Practice DSA problem", _ => "Open learning resource" };
}
