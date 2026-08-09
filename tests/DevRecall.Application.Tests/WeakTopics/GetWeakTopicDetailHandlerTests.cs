using DevRecall.Application.Common.Exceptions;
using DevRecall.Application.Identity;
using DevRecall.Application.WeakTopics.GetDetail;
using DevRecall.Application.WeakTopics.Recalculate;
using DevRecall.Application.WeakTopics.Signals;
using DevRecall.Domain.WeakTopics;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;

namespace DevRecall.Application.Tests.WeakTopics;

public sealed class GetWeakTopicDetailHandlerTests
{
    private static readonly DateTimeOffset CalculatedAt =
        new(2026, 9, 1, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task Handle_RebuildsAtPersistedTimeAndGroupsNewestFirst()
    {
        var context = new Context();
        context.Signals.Items =
        [
            new(WeaknessSignalType.ReviewAgain, CalculatedAt.AddDays(-20)),
            new(WeaknessSignalType.ReviewAgain, CalculatedAt.AddDays(-2)),
            new(WeaknessSignalType.ReviewGood, CalculatedAt.AddDays(-10))
        ];

        var result = await context.Handler.HandleAsync(
            new(context.Profile.ProfileId), CancellationToken.None);

        context.Signals.FromUtc.Should().Be(CalculatedAt.AddDays(-90));
        context.Signals.ToUtc.Should().Be(CalculatedAt);
        result.Contributions.Select(x => x.OccurredAtUtc).Should()
            .BeInDescendingOrder();
        result.SignalGroups.Single(x => x.SignalType == "ReviewAgain")
            .Count.Should().Be(2);
        result.Reasons.Single(x => x.Type == "ReviewAgain")
            .Count.Should().Be(2);
        result.LatestSignalAtUtc.Should().Be(CalculatedAt.AddDays(-2));
    }

    [Fact]
    public async Task Handle_BoundsRecentSignalsToTen()
    {
        var context = new Context();
        context.Signals.Items = Enumerable.Range(1, 12)
            .Select(day => new WeakTopicSignalReadModel(
                WeaknessSignalType.ReviewHard, CalculatedAt.AddDays(-day)))
            .ToArray();

        var result = await context.Handler.HandleAsync(
            new(context.Profile.ProfileId), CancellationToken.None);

        result.Contributions.Should().HaveCount(10);
        result.Contributions.Select(x => x.OccurredAtUtc).Should()
            .BeInDescendingOrder();
    }

    [Fact]
    public async Task Handle_NoSignalsReturnsEmptyExplanation()
    {
        var context = new Context();

        var result = await context.Handler.HandleAsync(
            new(context.Profile.ProfileId), CancellationToken.None);

        result.Contributions.Should().BeEmpty();
        result.SignalGroups.Should().BeEmpty();
        result.LatestSignalAtUtc.Should().BeNull();
    }

    [Fact]
    public async Task Handle_MissingResourceUsesFallback()
    {
        var context = new Context { Resource = null };

        var result = await context.Handler.HandleAsync(
            new(context.Profile.ProfileId), CancellationToken.None);

        result.ResourceTitle.Should().Be("Unavailable resource");
        result.IsResourceAvailable.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_CrossUserOrMissingProfileThrowsNotFound()
    {
        var context = new Context { ProfileResult = null };

        var action = () => context.Handler.HandleAsync(
            new(Guid.NewGuid()), CancellationToken.None);

        await action.Should().ThrowAsync<NotFoundException>()
            .Where(x => x.ErrorCode == WeakTopicErrors.ProfileNotFound.Code);
    }

    private sealed class Context
    {
        private readonly Guid userId = Guid.NewGuid();
        private readonly Guid resourceId = Guid.NewGuid();
        public DetailReaderStub DetailReader { get; }
        public SignalReaderStub Signals { get; } = new();
        public WeakTopicDetailReadModel Profile { get; }
        public WeakTopicDetailReadModel? ProfileResult
        {
            get => DetailReader.Profile;
            set => DetailReader.Profile = value;
        }
        public WeakTopicResourceReadModel? Resource { get; set; }

        public Context()
        {
            Profile = new(
                Guid.NewGuid(), WeakTopicResourceType.KnowledgeNode, resourceId,
                0, WeaknessLevel.None, 0, 1, CalculatedAt,
                CalculatedAt, CalculatedAt);
            DetailReader = new() { Profile = Profile };
            Resource = new(
                WeakTopicResourceType.KnowledgeNode, resourceId,
                "Node", "Preview", true);
        }

        public GetWeakTopicDetailHandler Handler => new(
            DetailReader, new ResourceReaderStub(() => Resource), Signals,
            new CurrentUserStub(userId),
            NullLogger<GetWeakTopicDetailHandler>.Instance);
    }

    private sealed class DetailReaderStub : IWeakTopicDetailReader
    {
        public WeakTopicDetailReadModel? Profile { get; set; }
        public Task<WeakTopicDetailReadModel?> FindAsync(
            Guid userId, Guid profileId, CancellationToken cancellationToken) =>
            Task.FromResult(Profile);
    }

    private sealed class SignalReaderStub : IWeakTopicSignalReader
    {
        public IReadOnlyList<WeakTopicSignalReadModel> Items { get; set; } = [];
        public DateTimeOffset FromUtc { get; private set; }
        public DateTimeOffset ToUtc { get; private set; }
        public Task<IReadOnlyList<WeakTopicSignalReadModel>> ReadAsync(
            Guid userId, WeakTopicResourceType resourceType, Guid resourceId,
            DateTimeOffset fromUtc, DateTimeOffset toUtc,
            CancellationToken cancellationToken)
        {
            FromUtc = fromUtc;
            ToUtc = toUtc;
            return Task.FromResult(Items);
        }
    }

    private sealed class ResourceReaderStub(
        Func<WeakTopicResourceReadModel?> resource) : IWeakTopicResourceReader
    {
        public Task<WeakTopicResourceReadModel?> FindAsync(
            Guid userId, WeakTopicResourceType resourceType, Guid resourceId,
            CancellationToken cancellationToken) => Task.FromResult(resource());
    }

    private sealed class CurrentUserStub(Guid userId) : ICurrentUser
    {
        public bool IsAuthenticated => true;
        public Guid? UserId => userId;
    }
}
