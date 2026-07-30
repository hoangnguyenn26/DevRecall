using DevRecall.Application.Common.Exceptions;
using DevRecall.Application.Common.Time;
using DevRecall.Application.Identity;
using DevRecall.Application.WeakTopics;
using DevRecall.Application.WeakTopics.Recalculate;
using DevRecall.Application.WeakTopics.Signals;
using DevRecall.Domain.WeakTopics;
using FluentAssertions;

namespace DevRecall.Application.Tests.WeakTopics;

public sealed class RecalculateWeakTopicHandlerTests
{
    private static readonly DateTimeOffset Now =
        new(2026, 9, 1, 12, 0, 0, TimeSpan.Zero);

    [Theory]
    [InlineData("knowledge node", WeakTopicResourceType.KnowledgeNode)]
    [InlineData("INTERVIEW-QUESTION", WeakTopicResourceType.InterviewQuestion)]
    [InlineData("dsa_problem", WeakTopicResourceType.DsaProblem)]
    public void ResourceTypeParser_AcceptsSupportedForms(
        string value, WeakTopicResourceType expected) =>
        WeakTopicResourceTypeParser.Parse(value).Should().Be(expected);

    [Theory]
    [InlineData("1")]
    [InlineData(" 1 ")]
    [InlineData("+1")]
    [InlineData("01")]
    [InlineData("unknown")]
    public void ResourceTypeParser_RejectsInvalidValues(string value) =>
        FluentActions.Invoking(() => WeakTopicResourceTypeParser.Parse(value))
            .Should().Throw<ValidationException>();

    [Fact]
    public async Task Handle_CreatesNoneProfileWithoutSignals()
    {
        var context = new Context();

        var result = await context.Handler.HandleAsync(
            new("KnowledgeNode", context.ResourceId), CancellationToken.None);

        result.WasCreated.Should().BeTrue();
        result.Score.Should().Be(0);
        result.Level.Should().Be("None");
        result.Version.Should().Be(1);
        context.Repository.SaveCount.Should().Be(1);
        context.Signals.FromUtc.Should().Be(Now.AddDays(-90));
        context.Signals.ToUtc.Should().Be(Now);
    }

    [Fact]
    public async Task Handle_ReturnsWeightedContributions()
    {
        var context = new Context();
        context.Signals.Items =
        [
            new(WeaknessSignalType.ReviewAgain, Now.AddDays(-2)),
            new(WeaknessSignalType.DsaFailed, Now.AddDays(-20))
        ];

        var result = await context.Handler.HandleAsync(
            new("KnowledgeNode", context.ResourceId), CancellationToken.None);

        result.Score.Should().Be(6.4m);
        result.Contributions.Should().HaveCount(2);
    }

    [Fact]
    public async Task Handle_UpdatesExistingProfileAndVersion()
    {
        var context = new Context();
        context.Repository.Profile = WeakTopicProfile.Create(
            Guid.NewGuid(), context.UserId, WeakTopicResourceType.KnowledgeNode,
            context.ResourceId, WeakTopicScoringPolicy.Calculate([], Now.AddDays(-1)),
            Now.AddDays(-1));
        context.Signals.Items =
        [
            new(WeaknessSignalType.ReviewAgain, Now)
        ];

        var result = await context.Handler.HandleAsync(
            new("KnowledgeNode", context.ResourceId), CancellationToken.None);

        result.WasCreated.Should().BeFalse();
        result.Version.Should().Be(2);
        result.Score.Should().Be(4);
    }

    [Fact]
    public async Task Handle_MissingOwnedResourceThrowsNotFound()
    {
        var context = new Context { Resource = null };

        var action = () => context.Handler.HandleAsync(
            new("KnowledgeNode", context.ResourceId), CancellationToken.None);

        await action.Should().ThrowAsync<NotFoundException>()
            .Where(x => x.ErrorCode == WeakTopicErrors.ResourceNotFound.Code);
    }

    private sealed class Context
    {
        public Guid UserId { get; } = Guid.NewGuid();
        public Guid ResourceId { get; } = Guid.NewGuid();
        public RepositoryStub Repository { get; } = new();
        public SignalReaderStub Signals { get; } = new();
        public WeakTopicResourceReadModel? Resource { get; set; }

        public Context()
        {
            Resource = new(
                WeakTopicResourceType.KnowledgeNode, ResourceId, "Node", null, true);
        }

        public RecalculateWeakTopicHandler Handler => new(
            Repository, Signals, new ResourceReaderStub(() => Resource),
            new CurrentUserStub(UserId), new ClockStub());
    }

    private sealed class RepositoryStub : IWeakTopicProfileRepository
    {
        public WeakTopicProfile? Profile { get; set; }
        public int SaveCount { get; private set; }
        public Task<WeakTopicProfile?> GetByUserAndResourceForUpdateAsync(
            Guid userId, WeakTopicResourceType resourceType, Guid resourceId,
            CancellationToken cancellationToken) => Task.FromResult(Profile);
        public void Add(WeakTopicProfile profile) => Profile = profile;
        public Task<IReadOnlyList<WeakTopicProfile>> GetByUserIdForUpdateAsync(
            Guid userId, CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<WeakTopicProfile>>(
                Profile is null ? [] : [Profile]);
        public Task SaveChangesAsync(CancellationToken cancellationToken)
        {
            SaveCount++;
            return Task.CompletedTask;
        }
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
        Func<WeakTopicResourceReadModel?> getResource) : IWeakTopicResourceReader
    {
        public Task<WeakTopicResourceReadModel?> FindAsync(
            Guid userId, WeakTopicResourceType resourceType, Guid resourceId,
            CancellationToken cancellationToken) => Task.FromResult(getResource());
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
