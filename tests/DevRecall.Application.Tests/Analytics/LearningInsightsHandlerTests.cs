using DevRecall.Application.Analytics;
using DevRecall.Application.Analytics.Insights;
using DevRecall.Application.Common.Time;
using DevRecall.Application.Identity;
using DevRecall.Application.Recommendations.GetList;
using DevRecall.Domain.Recommendations;
using DevRecall.Domain.WeakTopics;
using FluentAssertions;

namespace DevRecall.Application.Tests.Analytics;

public sealed class LearningInsightsHandlerTests
{
    private static readonly DateTimeOffset Now = new(2026, 8, 20, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task ActiveRecommendation_IsReusedAsAttentionAction()
    {
        var source = new SourceStub { Items = [CreateSource(Now.AddDays(-1))] };
        var result = await Create(source, new AnalyticsStub()).HandleAsync("7d", 10, CancellationToken.None);
        var insight = result.Items.Single();
        insight.Type.Should().Be("WeakTopicNeedsAttention");
        insight.Action!.Type.Should().Be("PracticeInterview");
        insight.Action.TargetId.Should().Be(source.Items[0].ResourceId);
        insight.Signals.Should().Contain(x => x.Type == "ContributingSignals" && x.Value == "4");
    }

    [Fact]
    public async Task StaleWeakTopicAndLowSamples_DoNotFabricateInsight()
    {
        var source = new SourceStub { Items = [CreateSource(Now.AddDays(-15))] };
        var result = await Create(source, new AnalyticsStub()).HandleAsync("7d", 10, CancellationToken.None);
        result.Items.Should().BeEmpty();
    }

    [Fact]
    public async Task InterviewTrend_PreservesSelfRatingLanguage()
    {
        var analytics = new AnalyticsStub
        {
            Performance = [(new(0, 0, 0, 0), new(0, 0, 2, 4), new(0, 0, 0, 0)), (new(0, 0, 0, 0), new(2, 2, 1, 1), new(0, 0, 0, 0))]
        };
        var result = await Create(new SourceStub(), analytics).HandleAsync("7d", 10, CancellationToken.None);
        result.Items.Single().Summary.Should().Contain("self-ratings");
    }

    private static LearningInsightSource CreateSource(DateTimeOffset calculatedAt) => new(Guid.NewGuid(), RecommendationResourceType.InterviewQuestion,
        Guid.NewGuid(), RecommendationType.PracticeInterview, RecommendationPriority.High, WeaknessLevel.Critical, 4, calculatedAt, Now.AddHours(-1));
    private static GetLearningInsightsHandler Create(SourceStub source, AnalyticsStub analytics) => new(source, new ResourceStub(), analytics, new UserStub(), new ClockStub());
    private sealed class ClockStub : IUtcClock { public DateTimeOffset UtcNow => Now; }
    private sealed class UserStub : ICurrentUser { public bool IsAuthenticated => true; public Guid? UserId { get; } = Guid.NewGuid(); }
    private sealed class SourceStub : ILearningInsightSourceReader { public IReadOnlyList<LearningInsightSource> Items { get; init; } = []; public Task<IReadOnlyList<LearningInsightSource>> ReadAsync(Guid userId, int take, DateTimeOffset now, CancellationToken cancellationToken) => Task.FromResult(Items); }
    private sealed class ResourceStub : IRecommendationResourceSummaryReader
    {
        public Task<IReadOnlyList<RecommendationResourceSummary>> ReadManyAsync(Guid userId, IReadOnlyCollection<RecommendationResourceReference> resources, CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<RecommendationResourceSummary>>(resources.Select(x => new RecommendationResourceSummary(x.ResourceType, x.ResourceId, "Service lifetimes", null, true)).ToArray());
    }
    private sealed class AnalyticsStub : IAnalyticsInsightsReader
    {
        private int index;
        public IReadOnlyList<(RatingDistribution Review, RatingDistribution Interview, RatingDistribution Dsa)> Performance { get; init; } = [(new(0, 0, 0, 0), new(0, 0, 0, 0), new(0, 0, 0, 0)), (new(0, 0, 0, 0), new(0, 0, 0, 0), new(0, 0, 0, 0))];
        public Task<(RatingDistribution Review, RatingDistribution Interview, RatingDistribution Dsa)> ReadPerformanceAsync(Guid userId, AnalyticsDateRange range, CancellationToken cancellationToken) => Task.FromResult(Performance[index++]);
        public Task<AnalyticsOverviewAggregate> ReadOverviewAsync(Guid userId, AnalyticsDateRange range, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<IReadOnlyList<AnalyticsActivityPoint>> ReadActivityAsync(Guid userId, AnalyticsDateRange range, CancellationToken cancellationToken) => throw new NotSupportedException();
    }
}
