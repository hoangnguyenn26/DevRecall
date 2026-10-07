using DevRecall.Application.LearningContent;
using DevRecall.Application.LearningProfiles;
using DevRecall.Domain.LearningContent;

namespace DevRecall.Application.Discover;

public sealed record DiscoverResourceCandidate(DiscoverCandidate Metadata, ExternalResourceKind ResourceKind, string SourceName);
public sealed record DiscoverResource(string Slug, string Title, string Summary, string ResourceKind, string SourceName,
    string Difficulty, int EstimatedMinutes, IReadOnlyList<LearningContentTechnologyItem> Technologies,
    IReadOnlyList<LearningContentTopicItem> Topics, IReadOnlyList<DiscoverReason> Reasons);

public static class ResourceRecommendationPolicy
{
    public const int CandidateLimit = 100;
    public const int RecommendedLimit = 4;

    public static IReadOnlyList<DiscoverResource> Build(DiscoverInputs inputs) => inputs.Resources
        .DistinctBy(item => item.Metadata.Id)
        // Reuse established signal matching and fit calculations, not the lesson candidate pool or top-N.
        .Select(item => (Item: item, Score: LearningRecommendationPolicy.Score(item.Metadata, inputs.Declared, inputs.WeakTopics)))
        .Where(item => item.Score.HasSemanticMatch)
        .OrderByDescending(item => item.Score.WeakTopic + item.Score.Goal + item.Score.Technology)
        .ThenByDescending(item => item.Score.Difficulty + item.Score.Time)
        .ThenByDescending(item => item.Item.Metadata.PublishedAtUtc).ThenBy(item => item.Item.Metadata.Id)
        .Take(RecommendedLimit).Select(item => Map(item.Item, item.Score.Reasons)).ToArray();

    private static DiscoverResource Map(DiscoverResourceCandidate resource, IReadOnlyList<DiscoverReason> reasons)
    {
        var item = resource.Metadata;
        return new(item.Slug, item.Title, item.Summary, resource.ResourceKind.ToString(), resource.SourceName,
            item.Difficulty.ToString(), item.EstimatedMinutes,
            item.Technologies.Order().Select(value => LearningProfileMetadata.TechnologyValue(value, false))
                .Select(value => new LearningContentTechnologyItem(value.Value, value.Label)).ToArray(),
            item.Topics.OrderBy(topic => topic.Name).Select(topic => new LearningContentTopicItem(topic.Slug, topic.Name)).ToArray(),
            reasons.Where(reason => reason.Type is "WeakTopicMatch" or "GoalMatch" or "PrimaryTechnologyMatch" or "SecondaryTechnologyMatch")
                .Take(2).ToArray());
    }
}
