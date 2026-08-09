using DevRecall.Application.Common.Exceptions;
using DevRecall.Application.Common.Time;
using DevRecall.Application.Identity;
using DevRecall.Application.StudyPlans.Convert;
using DevRecall.Application.StudyPlans.Resources;
using DevRecall.Domain.Study;
using DevRecall.Domain.StudyPlans;
using FluentAssertions;

namespace DevRecall.Application.Tests.StudyPlans;

public sealed class ConvertStudyPlanHandlerTests
{
    private static readonly DateTimeOffset Now =
        new(2026, 9, 25, 10, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task Handle_ShouldCreateAndStartSessionAndConvertPlanOnce()
    {
        var context = new Context(CreateReadyPlan(3));

        var result = await context.Handler.HandleAsync(
            new(context.Plan.Id, context.Plan.Version), CancellationToken.None);

        result.StudyPlanStatus.Should().Be("Converted");
        result.StudySessionStatus.Should().Be("InProgress");
        result.Title.Should().Be(context.Plan.Title);
        result.TotalPlannedDurationMinutes.Should().Be(60);
        result.ItemCount.Should().Be(3);
        result.StudySessionVersion.Should().Be(2);
        context.Persistence.Added!.Items.Select(x => x.ResourceId)
            .Should().Equal(context.Plan.Items.OrderBy(x => x.Position)
                .Select(x => x.ResourceId));
        context.Persistence.SaveCount.Should().Be(1);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Handle_DraftOrCancelledShouldNotCreateSession(bool cancel)
    {
        var plan = CreateDraftPlan(1);
        if (cancel)
        {
            plan.Cancel(plan.Version, Now.AddMinutes(2));
        }

        var context = new Context(plan);
        var action = () => context.Handler.HandleAsync(
            new(plan.Id, plan.Version), CancellationToken.None);

        await action.Should().ThrowAsync<ConflictException>()
            .Where(x => x.ErrorCode == StudyPlanErrors.NotReady.Code);
        context.Persistence.Added.Should().BeNull();
    }

    [Fact]
    public async Task Handle_UnavailableResourceShouldBlockWholeConversion()
    {
        var context = new Context(CreateReadyPlan(2));
        context.Resources.UnavailableId =
            context.Plan.Items.OrderBy(x => x.Position).Last().ResourceId;
        var action = () => context.Handler.HandleAsync(
            new(context.Plan.Id, context.Plan.Version), CancellationToken.None);

        await action.Should().ThrowAsync<ConflictException>()
            .Where(x => x.ErrorCode
                == StudyPlanErrors.ResourceUnavailable.Code);
        context.Plan.Status.Should().Be(StudyPlanStatus.Ready);
        context.Persistence.Added.Should().BeNull();
        context.Persistence.SaveCount.Should().Be(0);
    }

    [Fact]
    public async Task Handle_StaleVersionShouldFailBeforeResourceRead()
    {
        var context = new Context(CreateReadyPlan(1));
        var action = () => context.Handler.HandleAsync(
            new(context.Plan.Id, context.Plan.Version - 1),
            CancellationToken.None);

        await action.Should().ThrowAsync<ConflictException>()
            .Where(x => x.ErrorCode == StudyPlanErrors.Conflict.Code);
        context.Resources.ReadCount.Should().Be(0);
    }

    [Fact]
    public async Task Handle_ConvertedRetryShouldReturnExistingWithoutSaving()
    {
        var plan = CreateReadyPlan(1);
        var session = StudySession.CreateFromPlan(
            Guid.NewGuid(), plan.UserId, plan.Title,
            plan.TotalPlannedDurationMinutes,
            [
                new(
                    Guid.NewGuid(), StudyResourceType.KnowledgeNode,
                    plan.Items.Single().ResourceId)
            ],
            Now);
        plan.MarkConverted(plan.Version, session.Id, Now);
        var context = new Context(plan);
        context.Persistence.ExistingSession = session;

        var result = await context.Handler.HandleAsync(
            new(plan.Id, plan.Version), CancellationToken.None);

        result.StudySessionId.Should().Be(session.Id);
        context.Persistence.Added.Should().BeNull();
        context.Persistence.SaveCount.Should().Be(0);
        context.Resources.ReadCount.Should().Be(0);
    }

    [Fact]
    public async Task Handle_CrossUserShouldReturnNotFound()
    {
        var context = new Context(CreateReadyPlan(1));
        context.Persistence.ReturnNull = true;
        var action = () => context.Handler.HandleAsync(
            new(context.Plan.Id, context.Plan.Version), CancellationToken.None);

        await action.Should().ThrowAsync<NotFoundException>()
            .Where(x => x.ErrorCode == StudyPlanErrors.NotFound.Code);
    }

    [Fact]
    public async Task Handle_ShouldMapAtomicPersistenceConflict()
    {
        var context = new Context(CreateReadyPlan(1));
        context.Persistence.ThrowConflict = true;
        var action = () => context.Handler.HandleAsync(
            new(context.Plan.Id, context.Plan.Version), CancellationToken.None);

        await action.Should().ThrowAsync<ConflictException>()
            .Where(x => x.ErrorCode == StudyPlanErrors.Conflict.Code);
    }

    private sealed class Context
    {
        public Context(StudyPlan plan)
        {
            Plan = plan;
            Persistence = new(plan);
            Resources = new(plan);
        }

        public StudyPlan Plan { get; }
        public PersistenceStub Persistence { get; }
        public ResourceReaderStub Resources { get; }
        public ConvertStudyPlanHandler Handler => new(
            Persistence, Resources, new CurrentUserStub(Plan.UserId),
            new ClockStub());
    }

    private sealed class PersistenceStub(StudyPlan plan)
        : IStudyPlanConversionPersistence
    {
        public StudySession? Added { get; private set; }
        public StudySession? ExistingSession { get; set; }
        public int SaveCount { get; private set; }
        public bool ReturnNull { get; set; }
        public bool ThrowConflict { get; set; }
        public Task<StudyPlan?> GetPlanForUpdateAsync(
            Guid userId, Guid studyPlanId,
            CancellationToken cancellationToken) =>
            Task.FromResult(ReturnNull ? null : plan);
        public Task<StudySession?> GetStudySessionAsync(
            Guid userId, Guid studySessionId,
            CancellationToken cancellationToken) =>
            Task.FromResult(ExistingSession);
        public void AddStudySession(StudySession studySession) =>
            Added = studySession;
        public Task SaveChangesAsync(CancellationToken cancellationToken)
        {
            SaveCount++;
            return ThrowConflict
                ? Task.FromException(new StudyPlanConversionConflictException(
                    "conflict", new InvalidOperationException()))
                : Task.CompletedTask;
        }
    }

    private sealed class ResourceReaderStub(StudyPlan plan)
        : IStudyPlanResourceSummaryReader
    {
        public Guid? UnavailableId { get; set; }
        public int ReadCount { get; private set; }
        public Task<IReadOnlyList<StudyPlanResourceSummary>> ReadManyAsync(
            Guid userId,
            IReadOnlyCollection<StudyPlanResourceReference> resources,
            CancellationToken cancellationToken)
        {
            ReadCount++;
            return Task.FromResult<IReadOnlyList<StudyPlanResourceSummary>>(
                plan.Items.Select(item => new StudyPlanResourceSummary(
                    item.ResourceType, item.ResourceId, "Resource", null,
                    item.ResourceId != UnavailableId)).ToArray());
        }
    }

    private sealed class CurrentUserStub(Guid userId) : ICurrentUser
    {
        public bool IsAuthenticated => true;
        public Guid? UserId => userId;
    }

    private sealed class ClockStub : IUtcClock
    {
        public DateTimeOffset UtcNow => Now.AddHours(1);
    }

    private static StudyPlan CreateReadyPlan(int itemCount)
    {
        var plan = CreateDraftPlan(itemCount);
        plan.MarkReady(plan.Version, Now.AddMinutes(itemCount + 1));
        return plan;
    }

    private static StudyPlan CreateDraftPlan(int itemCount)
    {
        var items = Enumerable.Range(0, itemCount)
            .Select(_ => new InitialStudyPlanItem(
                Guid.NewGuid(), Guid.NewGuid(),
                StudyPlanResourceType.KnowledgeNode, Guid.NewGuid(), 20))
            .ToArray();
        return StudyPlan.CreateFromRecommendations(
            Guid.NewGuid(), Guid.NewGuid(), "Plan", items,
            Now, Now.AddDays(7));
    }
}
