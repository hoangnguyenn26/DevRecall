using DevRecall.Application.Common.Exceptions;
using DevRecall.Application.Common.Time;
using DevRecall.Application.Identity;
using DevRecall.Application.StudyPlans;
using DevRecall.Application.StudyPlans.Mutations;
using DevRecall.Domain.StudyPlans;
using FluentAssertions;

namespace DevRecall.Application.Tests.StudyPlans;

public sealed class StudyPlanMutationHandlerTests
{
    private static readonly DateTimeOffset Now =
        new(2026, 9, 24, 10, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task Rename_ShouldSaveChangedAndSkipNormalizedNoOp()
    {
        var context = new Context(CreatePlan());
        var first = await new UpdateStudyPlanHandler(
            context.Repository, context.User, context.Clock).HandleAsync(
            new(context.Plan.Id, "Renamed", context.Plan.Version),
            CancellationToken.None);
        var saves = context.Repository.SaveCount;
        var second = await new UpdateStudyPlanHandler(
            context.Repository, context.User, context.Clock).HandleAsync(
            new(context.Plan.Id, "  Renamed  ", first.Version),
            CancellationToken.None);

        first.Version.Should().Be(2);
        second.Version.Should().Be(first.Version);
        context.Repository.SaveCount.Should().Be(saves);
    }

    [Fact]
    public async Task UpdateItem_ShouldChangeDurationAndMapUnknownItemToNotFound()
    {
        var context = new Context(CreatePlan(withItems: 1));
        var item = context.Plan.Items.Single();
        var handler = new UpdateStudyPlanItemHandler(
            context.Repository, context.User, context.Clock);

        var result = await handler.HandleAsync(
            new(context.Plan.Id, item.Id, 30, context.Plan.Version),
            CancellationToken.None);
        var missing = () => handler.HandleAsync(
            new(context.Plan.Id, Guid.NewGuid(), 20, result.Version),
            CancellationToken.None);

        result.TotalPlannedDurationMinutes.Should().Be(30);
        await missing.Should().ThrowAsync<NotFoundException>()
            .Where(x => x.ErrorCode == StudyPlanErrors.ItemNotFound.Code);
    }

    [Fact]
    public async Task Remove_ShouldNormalizePositions()
    {
        var context = new Context(CreatePlan(withItems: 3));
        var middle = context.Plan.Items.Single(x => x.Position == 2);
        var handler = new RemoveStudyPlanItemHandler(
            context.Repository, context.User, context.Clock);

        await handler.HandleAsync(
            new(context.Plan.Id, middle.Id, context.Plan.Version),
            CancellationToken.None);

        context.Plan.Items.OrderBy(x => x.Position).Select(x => x.Position)
            .Should().Equal(1, 2);
    }

    [Fact]
    public async Task Reorder_ShouldSaveChangedAndSkipSameOrder()
    {
        var context = new Context(CreatePlan(withItems: 3));
        var handler = new ReorderStudyPlanItemsHandler(
            context.Repository, context.User, context.Clock);
        var reversed = context.Plan.Items.OrderByDescending(x => x.Position)
            .Select(x => x.Id).ToArray();

        var changed = await handler.HandleAsync(
            new(context.Plan.Id, reversed, context.Plan.Version),
            CancellationToken.None);
        var saves = context.Repository.SaveCount;
        await handler.HandleAsync(
            new(context.Plan.Id, reversed, changed.Version),
            CancellationToken.None);

        context.Plan.Items.OrderBy(x => x.Position).Select(x => x.Id)
            .Should().Equal(reversed);
        context.Repository.SaveCount.Should().Be(saves);
    }

    [Fact]
    public async Task MarkReady_ShouldRequireNonEmptyAndBeIdempotent()
    {
        var empty = new Context(CreatePlan());
        var invalid = () => new MarkStudyPlanReadyHandler(
            empty.Repository, empty.User, empty.Clock).HandleAsync(
            new(empty.Plan.Id, empty.Plan.Version), CancellationToken.None);
        await invalid.Should().ThrowAsync<ConflictException>()
            .Where(x => x.ErrorCode == StudyPlanErrors.EmptyPlan.Code);
        var context = new Context(CreatePlan(withItems: 1));
        var handler = new MarkStudyPlanReadyHandler(
            context.Repository, context.User, context.Clock);

        var ready = await handler.HandleAsync(
            new(context.Plan.Id, context.Plan.Version), CancellationToken.None);
        var saves = context.Repository.SaveCount;
        await handler.HandleAsync(
            new(context.Plan.Id, ready.Version), CancellationToken.None);

        context.Plan.Status.Should().Be(StudyPlanStatus.Ready);
        context.Repository.SaveCount.Should().Be(saves);
    }

    [Fact]
    public async Task Cancel_ShouldAllowDraftAndBeIdempotent()
    {
        var context = new Context(CreatePlan());
        var handler = new CancelStudyPlanHandler(
            context.Repository, context.User, context.Clock);

        var cancelled = await handler.HandleAsync(
            new(context.Plan.Id, context.Plan.Version), CancellationToken.None);
        var saves = context.Repository.SaveCount;
        await handler.HandleAsync(
            new(context.Plan.Id, cancelled.Version), CancellationToken.None);

        context.Plan.Status.Should().Be(StudyPlanStatus.Cancelled);
        context.Repository.SaveCount.Should().Be(saves);
    }

    [Fact]
    public async Task Mutation_ShouldMapStaleVersionAndCrossUserAsStableErrors()
    {
        var context = new Context(CreatePlan());
        var handler = new UpdateStudyPlanHandler(
            context.Repository, context.User, context.Clock);
        var stale = () => handler.HandleAsync(
            new(context.Plan.Id, "New", context.Plan.Version + 1),
            CancellationToken.None);
        context.Repository.ReturnNull = true;
        var missing = () => handler.HandleAsync(
            new(context.Plan.Id, "New", context.Plan.Version),
            CancellationToken.None);

        await missing.Should().ThrowAsync<NotFoundException>();
        context.Repository.ReturnNull = false;
        await stale.Should().ThrowAsync<ConflictException>()
            .Where(x => x.ErrorCode == StudyPlanErrors.Conflict.Code);
    }

    private sealed class Context(StudyPlan plan)
    {
        public StudyPlan Plan { get; } = plan;
        public CurrentUserStub User { get; } = new(plan.UserId);
        public ClockStub Clock { get; } = new();
        public RepositoryStub Repository { get; } = new(plan);
    }

    private sealed class RepositoryStub(StudyPlan plan) : IStudyPlanRepository
    {
        public int SaveCount { get; private set; }
        public bool ReturnNull { get; set; }
        public Task<StudyPlan?> GetByIdAndUserIdForUpdateAsync(
            Guid studyPlanId, Guid userId,
            CancellationToken cancellationToken) =>
            Task.FromResult(ReturnNull ? null : plan);
        public Task<StudyPlan?> GetDraftByUserIdForUpdateAsync(
            Guid userId, CancellationToken cancellationToken) =>
            Task.FromResult<StudyPlan?>(null);
        public void Add(StudyPlan studyPlan) { }
        public Task SaveChangesAsync(CancellationToken cancellationToken)
        {
            SaveCount++;
            return Task.CompletedTask;
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

    private static StudyPlan CreatePlan(int withItems = 0)
    {
        var plan = StudyPlan.Create(
            Guid.NewGuid(), Guid.NewGuid(), "Plan", Now, Now.AddDays(7));
        for (var index = 0; index < withItems; index++)
        {
            plan.AddRecommendationItem(
                Guid.NewGuid(), Guid.NewGuid(),
                StudyPlanResourceType.KnowledgeNode, Guid.NewGuid(), 20,
                Now.AddMinutes(index + 1));
        }

        return plan;
    }
}
