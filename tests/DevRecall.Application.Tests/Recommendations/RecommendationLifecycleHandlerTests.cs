using DevRecall.Application.Common.Exceptions;
using DevRecall.Application.Common.Time;
using DevRecall.Application.Identity;
using DevRecall.Application.Recommendations;
using DevRecall.Application.Recommendations.GetDetail;
using DevRecall.Application.Recommendations.GetList;
using DevRecall.Application.Recommendations.Lifecycle;
using DevRecall.Domain.Recommendations;
using DevRecall.Domain.WeakTopics;
using FluentAssertions;

namespace DevRecall.Application.Tests.Recommendations;

public sealed class RecommendationLifecycleHandlerTests
{
    private static readonly DateTimeOffset Now =
        new(2026, 9, 12, 10, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task Dismiss_ShouldTransitionAndIncrementVersion()
    {
        var context = new Context();

        var result = await context.Dismiss.HandleAsync(
            new(context.Item.Id, 1), CancellationToken.None);

        result.Status.Should().Be("Dismissed");
        result.Version.Should().Be(2);
        result.DismissedAtUtc.Should().Be(Now);
        context.Repository.SaveCount.Should().Be(1);
    }

    [Fact]
    public async Task Complete_SameActionWithCurrentVersionShouldBeNoOp()
    {
        var context = new Context();
        await context.Complete.HandleAsync(
            new(context.Item.Id, 1), CancellationToken.None);

        var result = await context.Complete.HandleAsync(
            new(context.Item.Id, 2), CancellationToken.None);

        result.Version.Should().Be(2);
        context.Repository.SaveCount.Should().Be(1);
    }

    [Fact]
    public async Task Complete_WithStaleVersionShouldConflictWithoutMutation()
    {
        var context = new Context();

        var action = () => context.Complete.HandleAsync(
            new(context.Item.Id, 2), CancellationToken.None);

        await action.Should().ThrowAsync<ConflictException>()
            .Where(x => x.ErrorCode == RecommendationErrors.Conflict.Code);
        context.Item.Status.Should().Be(RecommendationStatus.Active);
        context.Repository.SaveCount.Should().Be(0);
    }

    [Fact]
    public async Task IncompatibleTerminalActionShouldReturnNotActive()
    {
        var context = new Context();
        await context.Dismiss.HandleAsync(
            new(context.Item.Id, 1), CancellationToken.None);

        var action = () => context.Complete.HandleAsync(
            new(context.Item.Id, 2), CancellationToken.None);

        await action.Should().ThrowAsync<ConflictException>()
            .Where(x => x.ErrorCode == RecommendationErrors.NotActive.Code);
    }

    [Fact]
    public async Task Detail_ShouldUseFallbackAndRemainReadOnly()
    {
        var context = new Context();
        var detail = new DetailReaderStub
        {
            Item = new(
                context.Item.Id, context.Item.ResourceType,
                context.Item.ResourceId, context.Item.Type, context.Item.Priority,
                context.Item.PriorityScore, context.Item.Status,
                context.Item.Reason.WeaknessScore,
                context.Item.Reason.WeaknessLevel,
                context.Item.Reason.SignalCount,
                context.Item.Reason.WeaknessCalculatedAtUtc,
                context.Item.GeneratedAtUtc, context.Item.ExpiresAtUtc,
                null, null, null, null, context.Item.CreatedAtUtc,
                context.Item.UpdatedAtUtc, context.Item.Version)
        };
        var handler = new GetRecommendationDetailHandler(
            detail, new SummaryStub(), new CurrentUserStub(context.UserId));

        var result = await handler.HandleAsync(
            new(context.Item.Id), CancellationToken.None);

        result.ResourceTitle.Should().Be("Unavailable resource");
        context.Repository.SaveCount.Should().Be(0);
        context.Item.Version.Should().Be(1);
    }

    private sealed class Context
    {
        public Guid UserId { get; } = Guid.NewGuid();
        public StudyRecommendation Item { get; }
        public RepositoryStub Repository { get; }

        public Context()
        {
            Item = StudyRecommendation.Create(
                Guid.NewGuid(), UserId, RecommendationResourceType.DsaProblem,
                Guid.NewGuid(), RecommendationType.RetryDsaProblem,
                RecommendationPriority.High,
                RecommendationReason.Create(
                    9m, WeaknessLevel.High, 2, Now.AddDays(-1)),
                Now.AddDays(-1), Now.AddDays(13));
            Repository = new(Item);
        }

        public DismissRecommendationHandler Dismiss => new(
            Repository, new CurrentUserStub(UserId), new ClockStub());
        public CompleteRecommendationHandler Complete => new(
            Repository, new CurrentUserStub(UserId), new ClockStub());
    }

    private sealed class RepositoryStub(StudyRecommendation item)
        : IStudyRecommendationRepository
    {
        public int SaveCount { get; private set; }
        public Task<StudyRecommendation?> GetByIdAndUserIdForUpdateAsync(
            Guid recommendationId, Guid userId,
            CancellationToken cancellationToken) => Task.FromResult<
                StudyRecommendation?>(recommendationId == item.Id ? item : null);
        public Task<StudyRecommendation?> GetActiveByUserAndResourceForUpdateAsync(
            Guid userId, RecommendationResourceType resourceType, Guid resourceId,
            RecommendationType type, CancellationToken cancellationToken) =>
            Task.FromResult<StudyRecommendation?>(item);
        public Task<IReadOnlyList<StudyRecommendation>>
            GetActiveByUserIdForUpdateAsync(
                Guid userId, CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<StudyRecommendation>>([item]);
        public void Add(StudyRecommendation recommendation) { }
        public Task SaveChangesAsync(CancellationToken cancellationToken)
        {
            SaveCount++;
            return Task.CompletedTask;
        }
    }

    private sealed class DetailReaderStub : IRecommendationDetailReader
    {
        public RecommendationDetailReadModel? Item { get; init; }
        public Task<RecommendationDetailReadModel?> FindAsync(
            Guid userId, Guid recommendationId,
            CancellationToken cancellationToken) => Task.FromResult(Item);
    }

    private sealed class SummaryStub : IRecommendationResourceSummaryReader
    {
        public Task<IReadOnlyList<RecommendationResourceSummary>> ReadManyAsync(
            Guid userId,
            IReadOnlyCollection<RecommendationResourceReference> resources,
            CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<RecommendationResourceSummary>>([]);
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
