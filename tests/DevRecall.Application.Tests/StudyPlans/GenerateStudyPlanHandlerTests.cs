using DevRecall.Application.Common.Exceptions;
using DevRecall.Application.Common.Time;
using DevRecall.Application.Identity;
using DevRecall.Application.StudyPlans;
using DevRecall.Application.StudyPlans.Generate;
using DevRecall.Application.StudyPlans.Generation;
using DevRecall.Application.StudyPlans.Resources;
using DevRecall.Domain.Recommendations;
using DevRecall.Domain.StudyPlans;
using FluentAssertions;

namespace DevRecall.Application.Tests.StudyPlans;

public sealed class GenerateStudyPlanHandlerTests
{
    private static readonly DateTimeOffset Now =
        new(2026, 9, 22, 10, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task Handle_ShouldSelectWithinBudgetAndKeepInitialVersionOne()
    {
        var context = new Context();
        context.AddCandidate(RecommendationPriority.Critical, 12);
        context.AddCandidate(RecommendationPriority.High, 9);
        context.AddCandidate(RecommendationPriority.Medium, 6);
        context.AddCandidate(RecommendationPriority.Low, 3);

        var result = await context.Handler.HandleAsync(
            new("Evening Practice", 90, 100), CancellationToken.None);

        result.Items.Select(x => x.PlannedDurationMinutes)
            .Should().Equal(45, 30, 15);
        result.TotalPlannedDurationMinutes.Should().Be(90);
        result.Version.Should().Be(1);
        result.ExpiresAtUtc.Should().Be(Now.AddDays(7));
        context.Repository.SaveCount.Should().Be(1);
        context.Repository.Added!.Items.Should().OnlyContain(x =>
            x.CreatedAtUtc == Now && x.UpdatedAtUtc == Now);
    }

    [Fact]
    public async Task Handle_ShouldSkipUnavailableAndContinueSelection()
    {
        var context = new Context();
        var unavailable = context.AddCandidate(
            RecommendationPriority.Critical, 12, isAvailable: false);
        var available = context.AddCandidate(RecommendationPriority.High, 9);

        var result = await context.Handler.HandleAsync(
            new("Plan", 45, 100), CancellationToken.None);

        result.Items.Should().ContainSingle(x =>
            x.SourceRecommendationId == available.RecommendationId);
        result.Items.Should().NotContain(x =>
            x.SourceRecommendationId == unavailable.RecommendationId);
    }

    [Fact]
    public async Task Handle_ShouldSelectOnlyFirstDuplicateResource()
    {
        var context = new Context();
        var resourceId = Guid.NewGuid();
        var first = context.AddCandidate(
            RecommendationPriority.High, 10, resourceId: resourceId);
        context.AddCandidate(
            RecommendationPriority.Medium, 8, resourceId: resourceId);

        var result = await context.Handler.HandleAsync(
            new("Plan", 90, 100), CancellationToken.None);

        result.Items.Should().ContainSingle();
        result.Items[0].SourceRecommendationId.Should()
            .Be(first.RecommendationId);
    }

    [Fact]
    public async Task Handle_ExistingDraftShouldShortCircuit()
    {
        var context = new Context();
        context.Repository.Draft = StudyPlan.Create(
            Guid.NewGuid(), context.UserId, "Draft", Now, Now.AddDays(7));
        var action = () => context.Handler.HandleAsync(
            new("Plan", 90, 100), CancellationToken.None);

        await action.Should().ThrowAsync<ConflictException>()
            .Where(x => x.ErrorCode
                == StudyPlanErrors.DraftAlreadyExists.Code);
        context.Candidates.ReadCount.Should().Be(0);
        context.Repository.SaveCount.Should().Be(0);
    }

    [Fact]
    public async Task Handle_NoEligibleItemsShouldNotPersist()
    {
        var context = new Context();
        context.AddCandidate(
            RecommendationPriority.Critical, 12, isAvailable: false);
        var action = () => context.Handler.HandleAsync(
            new("Plan", 90, 100), CancellationToken.None);

        await action.Should().ThrowAsync<UnprocessableEntityException>()
            .Where(x => x.ErrorCode == StudyPlanErrors.NoEligibleItems.Code);
        context.Repository.Added.Should().BeNull();
        context.Repository.SaveCount.Should().Be(0);
    }

    [Theory]
    [InlineData("", 90, 100)]
    [InlineData("Plan", 4, 100)]
    [InlineData("Plan", 481, 100)]
    [InlineData("Plan", 90, 0)]
    [InlineData("Plan", 90, 101)]
    public async Task Handle_ShouldValidateRequest(
        string title, int duration, int maximum)
    {
        var context = new Context();
        var action = () => context.Handler.HandleAsync(
            new(title, duration, maximum), CancellationToken.None);

        await action.Should().ThrowAsync<ValidationException>();
    }

    [Fact]
    public async Task Handle_ShouldMapConcurrentDraftConflict()
    {
        var context = new Context();
        context.AddCandidate(RecommendationPriority.Low, 3);
        context.Repository.ThrowDuplicateDraft = true;
        var action = () => context.Handler.HandleAsync(
            new("Plan", 15, 100), CancellationToken.None);

        await action.Should().ThrowAsync<ConflictException>()
            .Where(x => x.ErrorCode
                == StudyPlanErrors.DraftAlreadyExists.Code);
    }

    private sealed class Context
    {
        public Guid UserId { get; } = Guid.NewGuid();
        public RepositoryStub Repository { get; } = new();
        public CandidateReaderStub Candidates { get; } = new();
        public ResourceReaderStub Resources { get; } = new();
        public GenerateStudyPlanHandler Handler => new(
            Repository, Candidates, Resources,
            new CurrentUserStub(UserId), new ClockStub());

        public StudyPlanRecommendationCandidate AddCandidate(
            RecommendationPriority priority, decimal score,
            bool isAvailable = true, Guid? resourceId = null)
        {
            var id = resourceId ?? Guid.NewGuid();
            var candidate = new StudyPlanRecommendationCandidate(
                Guid.NewGuid(), RecommendationResourceType.KnowledgeNode, id,
                RecommendationType.ReviewKnowledge, priority, score,
                Now.AddDays(-1), Now.AddDays(1), 1);
            Candidates.Items.Add(candidate);
            Resources.Items.Add(new(
                StudyPlanResourceType.KnowledgeNode, id,
                $"Resource {id}", "Preview", isAvailable));
            return candidate;
        }
    }

    private sealed class RepositoryStub : IStudyPlanRepository
    {
        public StudyPlan? Draft { get; set; }
        public StudyPlan? Added { get; private set; }
        public int SaveCount { get; private set; }
        public bool ThrowDuplicateDraft { get; set; }
        public Task<StudyPlan?> GetByIdAndUserIdForUpdateAsync(
            Guid studyPlanId, Guid userId,
            CancellationToken cancellationToken) => Task.FromResult<StudyPlan?>(null);
        public Task<StudyPlan?> GetDraftByUserIdForUpdateAsync(
            Guid userId, CancellationToken cancellationToken) =>
            Task.FromResult(Draft);
        public void Add(StudyPlan studyPlan) => Added = studyPlan;
        public Task SaveChangesAsync(CancellationToken cancellationToken)
        {
            SaveCount++;
            return ThrowDuplicateDraft
                ? Task.FromException(new DraftStudyPlanAlreadyExistsException(
                    "duplicate", new InvalidOperationException()))
                : Task.CompletedTask;
        }
    }

    private sealed class CandidateReaderStub
        : IStudyPlanRecommendationCandidateReader
    {
        public List<StudyPlanRecommendationCandidate> Items { get; } = [];
        public int ReadCount { get; private set; }
        public Task<IReadOnlyList<StudyPlanRecommendationCandidate>> ReadAsync(
            Guid userId, int take, DateTimeOffset currentUtc,
            CancellationToken cancellationToken)
        {
            ReadCount++;
            return Task.FromResult<IReadOnlyList<
                StudyPlanRecommendationCandidate>>(Items.Take(take).ToArray());
        }
    }

    private sealed class ResourceReaderStub : IStudyPlanResourceSummaryReader
    {
        public List<StudyPlanResourceSummary> Items { get; } = [];
        public Task<IReadOnlyList<StudyPlanResourceSummary>> ReadManyAsync(
            Guid userId,
            IReadOnlyCollection<StudyPlanResourceReference> resources,
            CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<StudyPlanResourceSummary>>(
                Items.Where(x => resources.Contains(
                    new(x.ResourceType, x.ResourceId))).ToArray());
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
