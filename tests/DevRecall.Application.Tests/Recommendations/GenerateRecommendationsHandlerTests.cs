using DevRecall.Application.Common.Exceptions;
using DevRecall.Application.Common.Time;
using DevRecall.Application.Identity;
using DevRecall.Application.Recommendations;
using DevRecall.Application.Recommendations.Generation;
using DevRecall.Domain.Recommendations;
using DevRecall.Domain.WeakTopics;
using FluentAssertions;

namespace DevRecall.Application.Tests.Recommendations;

public sealed class GenerateRecommendationsHandlerTests
{
    private static readonly DateTimeOffset Now =
        new(2026, 9, 10, 10, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task Handle_CreatesMappedRecommendationsAndSavesOnce()
    {
        var context = new Context();
        context.Candidates.Items =
        [
            Candidate(WeakTopicResourceType.KnowledgeNode, 3m, WeaknessLevel.Low),
            Candidate(WeakTopicResourceType.InterviewQuestion, 6m, WeaknessLevel.Medium),
            Candidate(WeakTopicResourceType.DsaProblem, 9m, WeaknessLevel.High)
        ];

        var result = await context.Handler.HandleAsync(new(100), CancellationToken.None);

        result.CreatedCount.Should().Be(3);
        result.LowPriorityCount.Should().Be(1);
        result.MediumPriorityCount.Should().Be(1);
        result.HighPriorityCount.Should().Be(1);
        context.Repository.SaveCount.Should().Be(1);
        context.Repository.Items.Select(x => x.Type).Should().Equal(
            RecommendationType.ReviewKnowledge,
            RecommendationType.PracticeInterview,
            RecommendationType.RetryDsaProblem);
        context.Repository.Items.Should().OnlyContain(x =>
            x.GeneratedAtUtc == Now
            && x.ExpiresAtUtc == Now.AddDays(14));
    }

    [Fact]
    public async Task Handle_RefreshesChangedAndLeavesIdenticalUnchanged()
    {
        var context = new Context();
        var unchanged = Candidate(
            WeakTopicResourceType.KnowledgeNode, 3m, WeaknessLevel.Low);
        var changed = Candidate(
            WeakTopicResourceType.DsaProblem, 12m, WeaknessLevel.Critical);
        context.Candidates.Items = [unchanged, changed];
        context.Repository.Items.Add(Create(
            context.UserId, unchanged, RecommendationPriority.Low));
        context.Repository.Items.Add(Create(
            context.UserId,
            changed with { WeaknessScore = 9m, WeaknessLevel = WeaknessLevel.High },
            RecommendationPriority.High));

        var result = await context.Handler.HandleAsync(new(100), CancellationToken.None);

        result.UpdatedCount.Should().Be(1);
        result.UnchangedCount.Should().Be(1);
        context.Repository.Items[0].Version.Should().Be(1);
        context.Repository.Items[0].GeneratedAtUtc.Should().Be(Now.AddDays(-1));
        context.Repository.Items[1].Priority.Should().Be(
            RecommendationPriority.Critical);
        context.Repository.SaveCount.Should().Be(1);
    }

    [Fact]
    public async Task Handle_NoCandidatesReturnsEmptyWithoutLoadingOrSaving()
    {
        var context = new Context();

        var result = await context.Handler.HandleAsync(new(100), CancellationToken.None);

        result.CandidateCount.Should().Be(0);
        context.Repository.ActiveReadCount.Should().Be(0);
        context.Repository.SaveCount.Should().Be(0);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(501)]
    public async Task Handle_RejectsInvalidMaximum(int maximum)
    {
        var context = new Context();
        var action = () => context.Handler.HandleAsync(
            new(maximum), CancellationToken.None);
        await action.Should().ThrowAsync<ValidationException>();
    }

    [Fact]
    public async Task Handle_MapsDuplicateConflict()
    {
        var context = new Context();
        context.Candidates.Items =
        [
            Candidate(WeakTopicResourceType.KnowledgeNode, 3m, WeaknessLevel.Low)
        ];
        context.Repository.ThrowDuplicate = true;

        var action = () => context.Handler.HandleAsync(new(1), CancellationToken.None);

        await action.Should().ThrowAsync<ConflictException>()
            .Where(x => x.ErrorCode == RecommendationErrors.AlreadyExists.Code);
    }

    private static RecommendationCandidate Candidate(
        WeakTopicResourceType type, decimal score, WeaknessLevel level) =>
        new(Guid.NewGuid(), type, Guid.NewGuid(), score, level, 2, Now.AddDays(-1));

    private static StudyRecommendation Create(
        Guid userId, RecommendationCandidate candidate,
        RecommendationPriority priority) =>
        StudyRecommendation.Create(
            Guid.NewGuid(), userId,
            RecommendationMappingPolicy.MapResourceType(candidate.ResourceType),
            candidate.ResourceId,
            RecommendationMappingPolicy.MapType(candidate.ResourceType),
            priority, RecommendationReason.Create(
                candidate.WeaknessScore, candidate.WeaknessLevel,
                candidate.SignalCount, candidate.WeaknessCalculatedAtUtc),
            Now.AddDays(-1), Now.AddDays(13));

    private sealed class Context
    {
        public Guid UserId { get; } = Guid.NewGuid();
        public CandidateReaderStub Candidates { get; } = new();
        public RepositoryStub Repository { get; } = new();
        public GenerateRecommendationsHandler Handler => new(
            Candidates, Repository, new CurrentUserStub(UserId), new ClockStub());
    }

    private sealed class CandidateReaderStub : IRecommendationCandidateReader
    {
        public IReadOnlyList<RecommendationCandidate> Items { get; set; } = [];
        public Task<IReadOnlyList<RecommendationCandidate>> ReadAsync(
            Guid userId, int take, CancellationToken cancellationToken) =>
            Task.FromResult(Items);
    }

    private sealed class RepositoryStub : IStudyRecommendationRepository
    {
        public List<StudyRecommendation> Items { get; } = [];
        public int SaveCount { get; private set; }
        public int ActiveReadCount { get; private set; }
        public bool ThrowDuplicate { get; set; }
        public Task<StudyRecommendation?> GetActiveByUserAndResourceForUpdateAsync(
            Guid userId, RecommendationResourceType resourceType, Guid resourceId,
            RecommendationType type, CancellationToken cancellationToken) =>
            Task.FromResult(Items.SingleOrDefault(x =>
                x.ResourceType == resourceType && x.ResourceId == resourceId
                && x.Type == type && x.Status == RecommendationStatus.Active));
        public Task<StudyRecommendation?> GetByIdAndUserIdForUpdateAsync(
            Guid recommendationId, Guid userId,
            CancellationToken cancellationToken) =>
            Task.FromResult(Items.SingleOrDefault(x => x.Id == recommendationId));
        public Task<IReadOnlyList<StudyRecommendation>>
            GetActiveByUserIdForUpdateAsync(
                Guid userId, CancellationToken cancellationToken)
        {
            ActiveReadCount++;
            return Task.FromResult<IReadOnlyList<StudyRecommendation>>(
                Items.Where(x => x.Status == RecommendationStatus.Active).ToArray());
        }
        public void Add(StudyRecommendation recommendation) =>
            Items.Add(recommendation);
        public Task SaveChangesAsync(CancellationToken cancellationToken)
        {
            SaveCount++;
            return ThrowDuplicate
                ? Task.FromException(new ActiveRecommendationAlreadyExistsException(
                    "duplicate", new InvalidOperationException()))
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
