using DevRecall.Application.Common.Time;
using DevRecall.Application.Identity;
using DevRecall.Application.Today;
using DevRecall.Domain.Recommendations;
using DevRecall.Domain.StudyPlans;
using FluentAssertions;
using DevRecall.Application.LearningContent;
using DevRecall.Application.Discover;

namespace DevRecall.Application.Tests.Today;

public sealed class TodayNextActionPolicyTests
{
    private readonly TodayNextActionPolicy _policy = new();

    [Fact]
    public void OngoingWork_ShouldDominatePlannedAndSuggestedWork()
    {
        var lesson = new DevRecall.Application.LearningContent.ContinueLearningContentItem(
            "middleware", "Middleware", "Pipeline", "Intermediate", 20, [], DateTimeOffset.UtcNow);
        var suggested = new DevRecall.Application.Discover.DiscoverLesson("options", "Options", "Configuration",
            "Intermediate", 20, [], [], []);
        var context = new TodayActionContext(null, Plan(StudyPlanStatus.Ready), 0, null, 0)
        { ContinueLesson = lesson, RecommendedLesson = suggested };
        _policy.SelectAction(context).Type.Should().Be(TodayActionType.ContinueLearning);
        _policy.SelectAction(context with { ActiveSession = Session() }).Type.Should().Be(TodayActionType.ContinueStudySession);
        _policy.SelectAction(context with { ContinueLesson = null }).Type.Should().Be(TodayActionType.StartStudyPlan);
        _policy.SelectAction(context with { ContinueLesson = null, HasActionablePlan = false }).Type.Should().Be(TodayActionType.LearnRecommendedContent);
        _policy.SelectAction(context with { ContinueLesson = null, StudyPlan = null, RecommendedLesson = null }).Type.Should().Be(TodayActionType.BrowseLearning);
    }

    [Fact]
    public void DueReviews_ShouldWinOverSessionPlanAndRecommendation()
    {
        var result = _policy.SelectAction(new(
            Session(), Plan(StudyPlanStatus.Ready), 9,
            Recommendation(RecommendationPriority.Critical), 3));

        result.Type.Should().Be(TodayActionType.StartReview);
    }

    [Fact]
    public void Reviews_ShouldWinOverReadyPlan()
    {
        var result = _policy.SelectAction(new(
            null, Plan(StudyPlanStatus.Ready), 9, null, 0));

        result.Type.Should().Be(TodayActionType.StartReview);
    }

    [Fact]
    public void Reviews_ShouldWinOverDraftPlan()
    {
        var result = _policy.SelectAction(new(
            null, Plan(StudyPlanStatus.Draft), 2, null, 0));

        result.Type.Should().Be(TodayActionType.StartReview);
    }

    [Fact]
    public void DraftPlan_ShouldWinOverRecommendation()
    {
        var result = _policy.SelectAction(new(
            null, Plan(StudyPlanStatus.Draft), 0,
            Recommendation(RecommendationPriority.Critical), 1));

        result.Type.Should().Be(TodayActionType.ContinueStudyPlan);
    }

    [Theory]
    [InlineData(RecommendationPriority.Critical)]
    [InlineData(RecommendationPriority.High)]
    public void ImportantRecommendation_ShouldBecomePrimaryAction(
        RecommendationPriority priority)
    {
        var result = _policy.SelectAction(new(
            null, null, 0, Recommendation(priority), 1));

        result.Type.Should().Be(TodayActionType.OpenRecommendation);
    }

    [Fact]
    public void LowRecommendation_ShouldProducePlanGeneration()
    {
        var result = _policy.SelectAction(new(
            null, null, 0, Recommendation(RecommendationPriority.Low), 2));

        result.Type.Should().Be(TodayActionType.GenerateStudyPlan);
    }

    [Fact]
    public void ActiveRecommendations_ShouldProducePlanGeneration()
    {
        var result = _policy.SelectAction(new(null, null, 0, null, 4));

        result.TargetPath.Should().Be("/app/study-plans?action=generate");
    }

    [Fact]
    public void EmptyDashboard_ShouldOfferLearningBrowse()
    {
        var result = _policy.SelectAction(new(null, null, 0, null, 0));

        result.Type.Should().Be(TodayActionType.BrowseLearning);
        result.TargetPath.Should().Be("/app/learn");
    }

    [Fact]
    public void EveryAction_ShouldUseATrustedInternalTarget()
    {
        var contexts = new TodayActionContext[]
        {
            new(Session(), null, 0, null, 0),
            new(null, Plan(StudyPlanStatus.Ready), 0, null, 0),
            new(null, null, 1, null, 0),
            new(null, Plan(StudyPlanStatus.Draft), 0, null, 0),
            new(null, null, 0, Recommendation(RecommendationPriority.High), 1),
            new(null, null, 0, null, 1),
            new(null, null, 0, null, 0)
        };

        contexts.Select(_policy.SelectAction)
            .Should().OnlyContain(action =>
                action.TargetPath.StartsWith("/app/", StringComparison.Ordinal)
                && !action.TargetPath.StartsWith("//", StringComparison.Ordinal));
    }

    [Fact]
    public void Selection_ShouldBeDeterministic()
    {
        var context = new TodayActionContext(
            null, Plan(StudyPlanStatus.Draft), 0,
            Recommendation(RecommendationPriority.High), 3);

        _policy.SelectAction(context).Should().Be(_policy.SelectAction(context));
    }

    [Fact]
    public async Task Handler_ShouldUseOneUtcInstantForReaderAndResponse()
    {
        var now = new DateTimeOffset(
            2026, 8, 4, 12, 0, 0, TimeSpan.Zero);
        var clock = new CountingClock(now);
        var reader = new ReaderStub();
        var recentReader = new RecentReaderStub();
        var handler = new GetTodayDashboardHandler(
            reader, _policy, recentReader,
            new CurrentUserStub(Guid.NewGuid()), clock, new EmptyLearningReader(), new EmptyDiscoverReader());

        var result = await handler.HandleAsync(
            new GetTodayDashboardQuery(), CancellationToken.None);

        clock.ReadCount.Should().Be(1);
        reader.CurrentUtc.Should().Be(now);
        recentReader.CurrentUtc.Should().Be(now);
        result.GeneratedAtUtc.Should().Be(now);
    }

    private static ActiveStudySessionCandidate Session() =>
        new(Guid.NewGuid(), "Active session", 2, null);

    private static TodayStudyPlanReadModel Plan(StudyPlanStatus status) =>
        new(Guid.NewGuid(), "Plan", status, 2, 45, 1, []);

    private static TodayRecommendationReadModel Recommendation(
        RecommendationPriority priority) =>
        new(
            Guid.NewGuid(), RecommendationResourceType.KnowledgeNode,
            Guid.NewGuid(), RecommendationType.ReviewKnowledge,
            priority, 80, "Dependency injection", "Review this topic.", true);

    private sealed class CountingClock(DateTimeOffset now) : IUtcClock
    {
        public int ReadCount { get; private set; }
        public DateTimeOffset UtcNow
        {
            get
            {
                ReadCount++;
                return now;
            }
        }
    }

    private sealed class CurrentUserStub(Guid userId) : ICurrentUser
    {
        public bool IsAuthenticated => true;
        public Guid? UserId => userId;
    }

    private sealed class ReaderStub : ITodayDashboardReader
    {
        public DateTimeOffset CurrentUtc { get; private set; }

        public Task<TodayDashboardReadModel> ReadAsync(
            Guid userId, DateTimeOffset currentUtc,
            CancellationToken cancellationToken)
        {
            CurrentUtc = currentUtc;
            return Task.FromResult(new TodayDashboardReadModel(
                "Learner", false, new(0, 0, 0, 5, 0), null, [], [],
                Enumerable.Range(0, 7).Select(index =>
                    new TodayActivityPointReadModel(
                        DateOnly.FromDateTime(currentUtc.UtcDateTime)
                            .AddDays(index), 0, 0)).ToArray(),
                null, 0));
        }
    }

    private sealed class RecentReaderStub : ITodayRecentActivityReader
    {
        public DateTimeOffset CurrentUtc { get; private set; }

        public Task<TodayRecentActivityReadModel?> ReadLatestAsync(
            Guid userId, DateTimeOffset currentUtc, CancellationToken cancellationToken)
        {
            CurrentUtc = currentUtc;
            return Task.FromResult<TodayRecentActivityReadModel?>(null);
        }
    }

    private sealed class EmptyDiscoverReader : IDiscoverReader
    {
        public Task<DiscoverInputs> GetLessonInputsAsync(Guid userId, CancellationToken cancellationToken) => GetAsync(userId, cancellationToken);
        public Task<DiscoverInputs> GetAsync(Guid userId, CancellationToken cancellationToken) =>
            Task.FromResult(new DiscoverInputs(new(null, null,
                new HashSet<DevRecall.Domain.LearningProfiles.Technology>(), new HashSet<DevRecall.Domain.LearningProfiles.Technology>(),
                new HashSet<DevRecall.Domain.LearningProfiles.LearningProfileGoal>(), null, false), [], []));
    }

    private sealed class EmptyLearningReader : ILearningContentReader
    {
        public Task<IReadOnlyList<ContinueLearningContentItem>> GetInProgressAsync(Guid userId, int take, CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<ContinueLearningContentItem>>([]);
        public Task<PublishedLearningContentPage> GetPublishedAsync(Guid userId, string? technology, string? topicSlug,
            string? difficulty, int skip, int take, CancellationToken cancellationToken, string? contentType = null) => throw new NotSupportedException();
        public Task<PublishedLearningContentDetail?> GetPublishedBySlugAsync(Guid userId, string slug, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<LearningContentHistoryPage> GetHistoryAsync(Guid userId, int skip, int take, CancellationToken cancellationToken) => throw new NotSupportedException();
    }
}
