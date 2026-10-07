using DevRecall.Application.Discover;
using DevRecall.Application.LearningProfiles;
using DevRecall.Domain.LearningContent;
using DevRecall.Domain.LearningProfiles;
using FluentAssertions;

namespace DevRecall.Application.Tests.Discover;

public sealed class ResourceRecommendationPolicyTests
{
    [Fact]
    public void Resources_ShouldRequireSemanticMatchUseSeparateStableTopNAndNeverExposeFitReasons()
    {
        var signals = new LearningProfileSignals(null, ExperienceLevel.Junior, new HashSet<Technology> { Technology.EfCore },
            new HashSet<Technology>(), new HashSet<LearningProfileGoal>(), 30, false);
        var now = DateTimeOffset.UtcNow;
        var resources = Enumerable.Range(1, 6).Select(index => new DiscoverResourceCandidate(new(
            Guid.Parse($"00000000-0000-0000-0000-{index:D12}"), $"resource-{index}", "EF docs", "Original summary",
            ContentDifficulty.Intermediate, 15, now, [Technology.EfCore], [], []), ExternalResourceKind.Documentation, "Microsoft Learn")).Reverse().ToArray();
        var inputs = new DiscoverInputs(signals, [], []) { Resources = [.. resources, resources[0]] };
        var result = ResourceRecommendationPolicy.Build(inputs);
        result.Select(item => item.Slug).Should().Equal("resource-1", "resource-2", "resource-3", "resource-4");
        result.Should().OnlyContain(item => item.Reasons.Count == 1 && item.Reasons[0].Type == "PrimaryTechnologyMatch");
        ResourceRecommendationPolicy.Build(inputs with { Resources = resources.Reverse().ToArray() })
            .Should().BeEquivalentTo(result, options => options.WithStrictOrdering());
        LearningRecommendationPolicy.Build(inputs).Recommended.Should().BeEmpty();
        ResourceRecommendationPolicy.Build(inputs with { Resources = [resources[0]] }).Should().ContainSingle();
        ResourceRecommendationPolicy.Build(inputs with { Declared = signals with { PrimaryTechnologies = new HashSet<Technology> { Technology.Vue } } })
            .Should().BeEmpty(); // Difficulty/time cannot pad the section.
        var topic = new DiscoverTopic(Guid.NewGuid(), "concurrency", "Concurrency");
        var weakOnly = inputs with
        {
            Declared = signals with { PrimaryTechnologies = new HashSet<Technology>() }, WeakTopics = [topic],
            Resources = [resources[0] with { Metadata = resources[0].Metadata with { Topics = [topic] } }]
        };
        ResourceRecommendationPolicy.Build(weakOnly).Should().ContainSingle(item => item.Reasons[0].Type == "WeakTopicMatch");
    }
}
