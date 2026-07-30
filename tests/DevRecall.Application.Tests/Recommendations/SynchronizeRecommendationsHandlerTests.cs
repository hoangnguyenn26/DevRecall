using DevRecall.Application.Common.Exceptions;
using DevRecall.Application.Common.Time;
using DevRecall.Application.Identity;
using DevRecall.Application.Recommendations;
using DevRecall.Application.Recommendations.GetList;
using DevRecall.Application.Recommendations.Synchronize;
using DevRecall.Domain.Recommendations;
using DevRecall.Domain.WeakTopics;
using FluentAssertions;

namespace DevRecall.Application.Tests.Recommendations;

public sealed class SynchronizeRecommendationsHandlerTests
{
    private static readonly DateTimeOffset Now =
        new(2026, 9, 15, 10, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task Handle_UsesDeterministicReasonPrecedenceAndSavesOnce()
    {
        var context = new Context();
        var allReasons = context.AddRecommendation(Now.AddDays(-1));
        context.WeakStates.Items =
        [
            Weak(allReasons, 0m, WeaknessLevel.None)
        ];

        var result = await context.Handler.HandleAsync(new(), CancellationToken.None);

        result.ResourceUnavailableCount.Should().Be(1);
        result.WeaknessResolvedCount.Should().Be(0);
        result.LifetimeElapsedCount.Should().Be(0);
        allReasons.ExpirationReason.Should().Be(
            RecommendationExpirationReason.ResourceUnavailable);
        context.Repository.SaveCount.Should().Be(1);
    }

    [Fact]
    public async Task Handle_ExpiresResolvedAndElapsedAndLeavesValidUnchanged()
    {
        var context = new Context();
        var resolved = context.AddRecommendation(Now.AddDays(1));
        var elapsed = context.AddRecommendation(Now);
        var valid = context.AddRecommendation(Now.AddDays(1));
        context.WeakStates.Items =
        [
            Weak(resolved, 0m, WeaknessLevel.None),
            Weak(elapsed, 8m, WeaknessLevel.High),
            Weak(valid, 8m, WeaknessLevel.High)
        ];
        context.Summaries.Items =
        [
            Available(resolved), Available(elapsed), Available(valid)
        ];

        var result = await context.Handler.HandleAsync(new(), CancellationToken.None);

        result.ExpiredRecommendations.Should().Be(2);
        result.UnchangedRecommendations.Should().Be(1);
        result.WeaknessResolvedCount.Should().Be(1);
        result.LifetimeElapsedCount.Should().Be(1);
        valid.Version.Should().Be(1);
        context.Repository.SaveCount.Should().Be(1);
    }

    [Fact]
    public async Task Handle_MissingWeakStateIsResolved()
    {
        var context = new Context();
        var item = context.AddRecommendation(Now.AddDays(1));
        context.Summaries.Items = [Available(item)];

        var result = await context.Handler.HandleAsync(new(), CancellationToken.None);

        result.WeaknessResolvedCount.Should().Be(1);
    }

    [Fact]
    public async Task Handle_NoActiveRecommendationsDoesNotReadOrSave()
    {
        var context = new Context();

        var result = await context.Handler.HandleAsync(new(), CancellationToken.None);

        result.ActiveRecommendationsChecked.Should().Be(0);
        context.WeakStates.ReadCount.Should().Be(0);
        context.Summaries.ReadCount.Should().Be(0);
        context.Repository.SaveCount.Should().Be(0);
    }

    [Fact]
    public async Task Handle_MapsPersistenceConflict()
    {
        var context = new Context();
        context.AddRecommendation(Now.AddDays(-1));
        context.Repository.ThrowConflict = true;

        var action = () => context.Handler.HandleAsync(new(), CancellationToken.None);

        await action.Should().ThrowAsync<ConflictException>()
            .Where(x => x.ErrorCode == RecommendationErrors.Conflict.Code);
    }

    private static CurrentWeakTopicState Weak(
        StudyRecommendation item, decimal score, WeaknessLevel level) =>
        new(Map(item.ResourceType), item.ResourceId, score, level);

    private static RecommendationResourceSummary Available(
        StudyRecommendation item) =>
        new(item.ResourceType, item.ResourceId, "Resource", null, true);

    private static WeakTopicResourceType Map(RecommendationResourceType type) =>
        type switch
        {
            RecommendationResourceType.KnowledgeNode =>
                WeakTopicResourceType.KnowledgeNode,
            RecommendationResourceType.InterviewQuestion =>
                WeakTopicResourceType.InterviewQuestion,
            RecommendationResourceType.DsaProblem =>
                WeakTopicResourceType.DsaProblem,
            _ => throw new ArgumentOutOfRangeException(nameof(type))
        };

    private sealed class Context
    {
        public Guid UserId { get; } = Guid.NewGuid();
        public RepositoryStub Repository { get; } = new();
        public WeakStateStub WeakStates { get; } = new();
        public SummaryStub Summaries { get; } = new();
        public SynchronizeRecommendationsHandler Handler => new(
            Repository, WeakStates, Summaries,
            new CurrentUserStub(UserId), new ClockStub());

        public StudyRecommendation AddRecommendation(DateTimeOffset expiresAt)
        {
            var item = StudyRecommendation.Create(
                Guid.NewGuid(), UserId, RecommendationResourceType.DsaProblem,
                Guid.NewGuid(), RecommendationType.RetryDsaProblem,
                RecommendationPriority.High,
                RecommendationReason.Create(
                    8m, WeaknessLevel.High, 2, Now.AddDays(-2)),
                Now.AddDays(-14), expiresAt);
            Repository.Items.Add(item);
            return item;
        }
    }

    private sealed class RepositoryStub : IStudyRecommendationRepository
    {
        public List<StudyRecommendation> Items { get; } = [];
        public int SaveCount { get; private set; }
        public bool ThrowConflict { get; set; }
        public Task<IReadOnlyList<StudyRecommendation>>
            GetActiveByUserIdForUpdateAsync(
                Guid userId, CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<StudyRecommendation>>(Items);
        public Task<StudyRecommendation?> GetActiveByUserAndResourceForUpdateAsync(
            Guid userId, RecommendationResourceType resourceType, Guid resourceId,
            RecommendationType type, CancellationToken cancellationToken) =>
            Task.FromResult<StudyRecommendation?>(null);
        public Task<StudyRecommendation?> GetByIdAndUserIdForUpdateAsync(
            Guid recommendationId, Guid userId,
            CancellationToken cancellationToken) =>
            Task.FromResult<StudyRecommendation?>(null);
        public void Add(StudyRecommendation recommendation) => Items.Add(recommendation);
        public Task SaveChangesAsync(CancellationToken cancellationToken)
        {
            SaveCount++;
            return ThrowConflict
                ? Task.FromException(new RecommendationPersistenceConflictException(
                    "conflict", new InvalidOperationException()))
                : Task.CompletedTask;
        }
    }

    private sealed class WeakStateStub : ICurrentWeakTopicStateReader
    {
        public IReadOnlyList<CurrentWeakTopicState> Items { get; set; } = [];
        public int ReadCount { get; private set; }
        public Task<IReadOnlyList<CurrentWeakTopicState>> ReadManyAsync(
            Guid userId,
            IReadOnlyCollection<RecommendationResourceReference> resources,
            CancellationToken cancellationToken)
        {
            ReadCount++;
            return Task.FromResult(Items);
        }
    }

    private sealed class SummaryStub : IRecommendationResourceSummaryReader
    {
        public IReadOnlyList<RecommendationResourceSummary> Items { get; set; } = [];
        public int ReadCount { get; private set; }
        public Task<IReadOnlyList<RecommendationResourceSummary>> ReadManyAsync(
            Guid userId,
            IReadOnlyCollection<RecommendationResourceReference> resources,
            CancellationToken cancellationToken)
        {
            ReadCount++;
            return Task.FromResult(Items);
        }
    }

    private sealed class CurrentUserStub(Guid userId) : ICurrentUser
    {
        public bool IsAuthenticated => true;
        public Guid? UserId => userId;
    }

    private sealed class ClockStub : IUtcClock
    {
        public DateTimeOffset UtcNow => Now;
    }
}
