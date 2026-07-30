using DevRecall.Application.Common.Exceptions;
using DevRecall.Application.Common.Time;
using DevRecall.Application.Identity;
using DevRecall.Application.WeakTopics;
using DevRecall.Application.WeakTopics.RecalculateAll;
using DevRecall.Domain.WeakTopics;
using FluentAssertions;

namespace DevRecall.Application.Tests.WeakTopics;

public sealed class RecalculateAllWeakTopicsHandlerTests
{
    private static readonly DateTimeOffset Now =
        new(2026, 9, 5, 10, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task Handle_CreatesAllCandidatesWithOneTimestampAndSave()
    {
        var context = new Context();
        context.Candidates.Items =
        [
            Candidate(WeakTopicResourceType.KnowledgeNode),
            Candidate(WeakTopicResourceType.DsaProblem)
        ];
        context.Signals.Items =
        [
            Signal(context.Candidates.Items[1], WeaknessSignalType.DsaFailed)
        ];

        var result = await context.Handler.HandleAsync(
            new(), CancellationToken.None);

        result.CandidateResources.Should().Be(2);
        result.CreatedProfiles.Should().Be(2);
        result.NoneProfiles.Should().Be(1);
        result.MediumProfiles.Should().Be(1);
        context.Repository.Profiles.Should().OnlyContain(
            x => x.CalculatedAtUtc == Now);
        context.Repository.SaveCount.Should().Be(1);
        context.Candidates.FromUtc.Should().Be(Now.AddDays(-90));
    }

    [Fact]
    public async Task Handle_UnchangedProfilesDoNotWriteOrIncreaseVersion()
    {
        var context = new Context();
        var candidate = Candidate(WeakTopicResourceType.KnowledgeNode);
        var profile = CreateProfile(context.UserId, candidate, 0m);
        context.Candidates.Items = [candidate];
        context.Repository.Profiles.Add(profile);

        var result = await context.Handler.HandleAsync(
            new(), CancellationToken.None);

        result.UnchangedProfiles.Should().Be(1);
        profile.Version.Should().Be(1);
        profile.CalculatedAtUtc.Should().Be(Now.AddDays(-1));
        context.Repository.SaveCount.Should().Be(0);
    }

    [Fact]
    public async Task Handle_ChangedProfileUpdatesOnceAndOldSignalProfileBecomesNone()
    {
        var context = new Context();
        var candidate = Candidate(WeakTopicResourceType.DsaProblem);
        var profile = CreateProfile(context.UserId, candidate, 8m);
        context.Candidates.Items = [candidate];
        context.Repository.Profiles.Add(profile);

        var result = await context.Handler.HandleAsync(
            new(), CancellationToken.None);

        result.UpdatedProfiles.Should().Be(1);
        result.NoneProfiles.Should().Be(1);
        profile.Version.Should().Be(2);
        context.Repository.SaveCount.Should().Be(1);
    }

    [Fact]
    public async Task Handle_RejectsMoreThanMaximumBeforeReadsOrMutation()
    {
        var context = new Context();
        context.Candidates.Items = Enumerable.Range(0, 501)
            .Select(_ => Candidate(WeakTopicResourceType.KnowledgeNode)).ToArray();

        var action = () => context.Handler.HandleAsync(
            new(), CancellationToken.None);

        await action.Should().ThrowAsync<ConflictException>()
            .Where(x => x.ErrorCode == WeakTopicErrors.BatchTooLarge.Code);
        context.Signals.ReadCount.Should().Be(0);
        context.Repository.SaveCount.Should().Be(0);
    }

    [Fact]
    public async Task Handle_MapsRepositoryConflictToBatchConflict()
    {
        var context = new Context();
        context.Candidates.Items = [Candidate(WeakTopicResourceType.KnowledgeNode)];
        context.Repository.ThrowConflict = true;

        var action = () => context.Handler.HandleAsync(
            new(), CancellationToken.None);

        await action.Should().ThrowAsync<ConflictException>()
            .Where(x => x.ErrorCode == WeakTopicErrors.BatchConflict.Code);
    }

    private static WeakTopicCandidate Candidate(WeakTopicResourceType type) =>
        new(type, Guid.NewGuid());

    private static WeakTopicBatchSignalReadModel Signal(
        WeakTopicCandidate candidate, WeaknessSignalType type) =>
        new(candidate.ResourceType, candidate.ResourceId, type, Now.AddDays(-1));

    private static WeakTopicProfile CreateProfile(
        Guid userId, WeakTopicCandidate candidate, decimal score)
    {
        var calculatedAt = Now.AddDays(-1);
        return WeakTopicProfile.Create(
            Guid.NewGuid(), userId, candidate.ResourceType, candidate.ResourceId,
            new(score, score, WeakTopicScoringPolicy.GetLevel(score),
                score == 0 ? 0 : 1, calculatedAt, []), calculatedAt);
    }

    private sealed class Context
    {
        public Guid UserId { get; } = Guid.NewGuid();
        public CandidateReaderStub Candidates { get; } = new();
        public SignalReaderStub Signals { get; } = new();
        public RepositoryStub Repository { get; } = new();
        public RecalculateAllWeakTopicsHandler Handler => new(
            Candidates, Signals, Repository, new CurrentUserStub(UserId),
            new ClockStub());
    }

    private sealed class CandidateReaderStub : IWeakTopicCandidateReader
    {
        public WeakTopicCandidate[] Items { get; set; } = [];
        public DateTimeOffset FromUtc { get; private set; }
        public Task<IReadOnlyList<WeakTopicCandidate>> ReadAsync(
            Guid userId, DateTimeOffset fromUtc, DateTimeOffset toUtc,
            CancellationToken cancellationToken)
        {
            FromUtc = fromUtc;
            return Task.FromResult<IReadOnlyList<WeakTopicCandidate>>(Items);
        }
    }

    private sealed class SignalReaderStub : IWeakTopicBatchSignalReader
    {
        public IReadOnlyList<WeakTopicBatchSignalReadModel> Items { get; set; } = [];
        public int ReadCount { get; private set; }
        public Task<IReadOnlyList<WeakTopicBatchSignalReadModel>> ReadAsync(
            Guid userId, DateTimeOffset fromUtc, DateTimeOffset toUtc,
            CancellationToken cancellationToken)
        {
            ReadCount++;
            return Task.FromResult(Items);
        }
    }

    private sealed class RepositoryStub : IWeakTopicProfileRepository
    {
        public List<WeakTopicProfile> Profiles { get; } = [];
        public int SaveCount { get; private set; }
        public bool ThrowConflict { get; set; }
        public Task<WeakTopicProfile?> GetByUserAndResourceForUpdateAsync(
            Guid userId, WeakTopicResourceType resourceType, Guid resourceId,
            CancellationToken cancellationToken) =>
            Task.FromResult(Profiles.SingleOrDefault(
                x => x.ResourceType == resourceType && x.ResourceId == resourceId));
        public Task<IReadOnlyList<WeakTopicProfile>> GetByUserIdForUpdateAsync(
            Guid userId, CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<WeakTopicProfile>>(Profiles);
        public void Add(WeakTopicProfile profile) => Profiles.Add(profile);
        public Task SaveChangesAsync(CancellationToken cancellationToken)
        {
            SaveCount++;
            return ThrowConflict
                ? Task.FromException(new WeakTopicProfileConflictException())
                : Task.CompletedTask;
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
