using DevRecall.Application.StudyPlans;
using DevRecall.Application.StudyPlans.Convert;
using DevRecall.Application.StudyPlans.Generation;
using DevRecall.Application.StudyPlans.GetDetail;
using DevRecall.Application.StudyPlans.GetList;
using DevRecall.Domain.Identity;
using DevRecall.Domain.Recommendations;
using DevRecall.Domain.Study;
using DevRecall.Domain.StudyPlans;
using DevRecall.Domain.WeakTopics;
using DevRecall.Infrastructure;
using DevRecall.IntegrationTests.Infrastructure;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace DevRecall.IntegrationTests.Persistence;

[Collection(PostgreSqlTestSuite.Name)]
public sealed class StudyPlanPersistenceTests(PostgreSqlFixture fixture)
{
    private static readonly DateTimeOffset Now =
        new(2026, 9, 20, 10, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task ShouldPersistAndReloadPlanWithOrderedItems()
    {
        var user = CreateUser();
        var plan = CreatePlan(user.Id);
        AddItem(plan, StudyPlanResourceType.KnowledgeNode, 15);
        AddItem(plan, StudyPlanResourceType.InterviewQuestion, 20);
        AddItem(plan, StudyPlanResourceType.DsaProblem, 30);
        await using var context = fixture.CreateDbContext();
        context.Users.Add(user);
        context.StudyPlans.Add(plan);
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        var stored = await context.StudyPlans.AsNoTracking()
            .Include(x => x.Items).SingleAsync(x => x.Id == plan.Id);

        stored.Title.Should().Be("Evening Practice");
        stored.Status.Should().Be(StudyPlanStatus.Draft);
        stored.Version.Should().Be(4);
        stored.Items.OrderBy(x => x.Position).Select(x => x.Position)
            .Should().Equal(1, 2, 3);
        stored.TotalPlannedDurationMinutes.Should().Be(65);
    }

    [Fact]
    public async Task Repository_ShouldMapSingleDraftConstraint()
    {
        var user = CreateUser();
        await using (var setup = fixture.CreateDbContext())
        {
            setup.Users.Add(user);
            await setup.SaveChangesAsync();
        }

        using var firstProvider = CreateServices();
        var first = firstProvider.GetRequiredService<IStudyPlanRepository>();
        first.Add(CreatePlan(user.Id));
        await first.SaveChangesAsync(CancellationToken.None);
        using var secondProvider = CreateServices();
        var second = secondProvider.GetRequiredService<IStudyPlanRepository>();
        second.Add(CreatePlan(user.Id));

        var duplicate = () => second.SaveChangesAsync(CancellationToken.None);

        await duplicate.Should()
            .ThrowAsync<DraftStudyPlanAlreadyExistsException>();
    }

    [Fact]
    public async Task Repository_ShouldLoadItemsAndMapConcurrencyConflict()
    {
        var user = CreateUser();
        var plan = CreatePlan(user.Id);
        AddItem(plan, StudyPlanResourceType.KnowledgeNode, 15);
        AddItem(plan, StudyPlanResourceType.DsaProblem, 30);
        await using (var setup = fixture.CreateDbContext())
        {
            setup.Users.Add(user);
            setup.StudyPlans.Add(plan);
            await setup.SaveChangesAsync();
        }

        using var providerA = CreateServices();
        using var providerB = CreateServices();
        var repositoryA = providerA.GetRequiredService<IStudyPlanRepository>();
        var repositoryB = providerB.GetRequiredService<IStudyPlanRepository>();
        var planA = await repositoryA.GetByIdAndUserIdForUpdateAsync(
            plan.Id, user.Id, CancellationToken.None);
        var planB = await repositoryB.GetDraftByUserIdForUpdateAsync(
            user.Id, CancellationToken.None);
        var firstItem = planA!.Items.OrderBy(x => x.Position).First();
        planA.UpdateItemDuration(firstItem.Id, 20, Now.AddHours(1));
        await repositoryA.SaveChangesAsync(CancellationToken.None);
        var secondItem = planB!.Items.OrderBy(x => x.Position).Last();
        planB.UpdateItemDuration(secondItem.Id, 35, Now.AddHours(1));

        var conflict = () => repositoryB.SaveChangesAsync(CancellationToken.None);

        await conflict.Should().ThrowAsync<StudyPlanPersistenceConflictException>();
    }

    [Fact]
    public async Task CandidateReader_ShouldScopeFilterBoundaryAndOrder()
    {
        var owner = CreateUser();
        var other = CreateUser();
        var criticalLower = CreateRecommendation(
            owner.Id, 12m, Now.AddDays(-1), Now.AddMinutes(1));
        var criticalHigher = CreateRecommendation(
            owner.Id, 13m, Now.AddDays(-5), null);
        var high = CreateRecommendation(
            owner.Id, 11m, Now.AddDays(1), null);
        var boundary = CreateRecommendation(
            owner.Id, 10m, Now.AddDays(-2), Now);
        var dismissed = CreateRecommendation(
            owner.Id, 20m, Now.AddDays(-3), null);
        dismissed.Dismiss(dismissed.Version, Now.AddHours(-1));
        await using (var context = fixture.CreateDbContext())
        {
            context.Users.AddRange(owner, other);
            context.StudyRecommendations.AddRange(
                criticalLower, criticalHigher, high, boundary, dismissed,
                CreateRecommendation(other.Id, 30m, Now, null));
            await context.SaveChangesAsync();
        }

        using var provider = CreateServices();
        var reader = provider
            .GetRequiredService<IStudyPlanRecommendationCandidateReader>();
        var candidates = await reader.ReadAsync(
            owner.Id, 100, Now, CancellationToken.None);

        candidates.Select(x => x.RecommendationId).Should()
            .Equal(criticalHigher.Id, criticalLower.Id, high.Id);
        candidates.Should().OnlyContain(x =>
            x.ExpiresAtUtc == null || x.ExpiresAtUtc > Now);
    }

    [Fact]
    public async Task UserAndPlanDeletion_ShouldCascadeToPlansAndItems()
    {
        var firstUser = CreateUser();
        var firstPlan = CreatePlan(firstUser.Id);
        AddItem(firstPlan, StudyPlanResourceType.KnowledgeNode, 15);
        var secondUser = CreateUser();
        var secondPlan = CreatePlan(secondUser.Id);
        AddItem(secondPlan, StudyPlanResourceType.DsaProblem, 30);
        await using var context = fixture.CreateDbContext();
        context.Users.AddRange(firstUser, secondUser);
        context.StudyPlans.AddRange(firstPlan, secondPlan);
        await context.SaveChangesAsync();

        context.StudyPlans.Remove(secondPlan);
        context.Users.Remove(firstUser);
        await context.SaveChangesAsync();

        (await context.StudyPlans.AnyAsync(x =>
            x.Id == firstPlan.Id || x.Id == secondPlan.Id))
            .Should().BeFalse();
        (await context.StudyPlanItems.AnyAsync(x =>
            x.StudyPlanId == firstPlan.Id || x.StudyPlanId == secondPlan.Id))
            .Should().BeFalse();
    }

    [Fact]
    public async Task ListReader_ShouldScopeFilterAndHandleEmptyPlanTotals()
    {
        var owner = CreateUser();
        var other = CreateUser();
        var emptyDraft = CreatePlan(owner.Id);
        var ready = StudyPlan.Create(
            Guid.NewGuid(), owner.Id, "Ready", Now.AddHours(-1),
            Now.AddDays(7));
        AddItem(ready, StudyPlanResourceType.DsaProblem, 30);
        ready.MarkReady(ready.Version, Now.AddMinutes(1));
        var otherPlan = CreatePlan(other.Id);
        await using (var context = fixture.CreateDbContext())
        {
            context.Users.AddRange(owner, other);
            context.StudyPlans.AddRange(emptyDraft, ready, otherPlan);
            await context.SaveChangesAsync();
        }

        using var provider = CreateServices();
        var reader = provider.GetRequiredService<IStudyPlanListReader>();
        var all = await reader.ReadAsync(
            owner.Id, null, 0, 10, CancellationToken.None);
        var filtered = await reader.ReadAsync(
            owner.Id, StudyPlanStatus.Ready, 0, 10, CancellationToken.None);

        all.TotalCount.Should().Be(2);
        all.Items.Should().Contain(x =>
            x.StudyPlanId == emptyDraft.Id && x.ItemCount == 0
            && x.TotalPlannedDurationMinutes == 0);
        filtered.Items.Should().ContainSingle(x =>
            x.StudyPlanId == ready.Id && x.TotalPlannedDurationMinutes == 30);
    }

    [Fact]
    public async Task DetailReader_ShouldBeOwnerScopedAndReturnOrderedItems()
    {
        var owner = CreateUser();
        var other = CreateUser();
        var plan = CreatePlan(owner.Id);
        AddItem(plan, StudyPlanResourceType.KnowledgeNode, 15);
        AddItem(plan, StudyPlanResourceType.InterviewQuestion, 20);
        await using (var context = fixture.CreateDbContext())
        {
            context.Users.AddRange(owner, other);
            context.StudyPlans.Add(plan);
            await context.SaveChangesAsync();
        }

        using var provider = CreateServices();
        var reader = provider.GetRequiredService<IStudyPlanDetailReader>();
        var owned = await reader.FindAsync(
            owner.Id, plan.Id, CancellationToken.None);
        var crossUser = await reader.FindAsync(
            other.Id, plan.Id, CancellationToken.None);

        owned.Should().NotBeNull();
        owned!.Items.Select(x => x.Position).Should().Equal(1, 2);
        crossUser.Should().BeNull();
    }

    [Fact]
    public async Task Repository_ShouldPersistReorderedPositions()
    {
        var user = CreateUser();
        var plan = CreatePlan(user.Id);
        AddItem(plan, StudyPlanResourceType.KnowledgeNode, 15);
        AddItem(plan, StudyPlanResourceType.InterviewQuestion, 20);
        AddItem(plan, StudyPlanResourceType.DsaProblem, 30);
        await using (var setup = fixture.CreateDbContext())
        {
            setup.Users.Add(user);
            setup.StudyPlans.Add(plan);
            await setup.SaveChangesAsync();
        }

        using var provider = CreateServices();
        var repository = provider.GetRequiredService<IStudyPlanRepository>();
        var stored = await repository.GetByIdAndUserIdForUpdateAsync(
            plan.Id, user.Id, CancellationToken.None);
        var reordered = stored!.Items.OrderByDescending(x => x.Position)
            .Select(x => x.Id).ToArray();
        stored.ReorderItems(reordered, stored.Version, Now.AddHours(1));
        await repository.SaveChangesAsync(CancellationToken.None);

        await using var verify = fixture.CreateDbContext();
        var positions = await verify.StudyPlanItems.AsNoTracking()
            .Where(x => x.StudyPlanId == plan.Id)
            .OrderBy(x => x.Position).Select(x => x.Id).ToArrayAsync();
        positions.Should().Equal(reordered);
    }

    [Fact]
    public async Task ConversionPersistence_ShouldSavePlanAndSessionAtomically()
    {
        var user = CreateUser();
        var plan = CreatePlan(user.Id);
        AddItem(plan, StudyPlanResourceType.KnowledgeNode, 15);
        AddItem(plan, StudyPlanResourceType.DsaProblem, 30);
        plan.MarkReady(plan.Version, Now.AddHours(1));
        await using (var setup = fixture.CreateDbContext())
        {
            setup.Users.Add(user);
            setup.StudyPlans.Add(plan);
            await setup.SaveChangesAsync();
        }

        using var provider = CreateServices();
        var persistence =
            provider.GetRequiredService<IStudyPlanConversionPersistence>();
        var stored = await persistence.GetPlanForUpdateAsync(
            user.Id, plan.Id, CancellationToken.None);
        var session = CreateSession(stored!, Guid.NewGuid());
        stored!.MarkConverted(
            stored.Version, session.Id, Now.AddHours(2));
        persistence.AddStudySession(session);
        await persistence.SaveChangesAsync(CancellationToken.None);

        await using var verify = fixture.CreateDbContext();
        var converted = await verify.StudyPlans.AsNoTracking()
            .SingleAsync(x => x.Id == plan.Id);
        var savedSession = await verify.StudySessions.AsNoTracking()
            .Include(x => x.Items).SingleAsync(x => x.Id == session.Id);
        converted.Status.Should().Be(StudyPlanStatus.Converted);
        converted.ConvertedStudySessionId.Should().Be(session.Id);
        savedSession.Status.Should().Be(StudySessionStatus.Planned);
        savedSession.Version.Should().Be(1);
        savedSession.Items.Should().HaveCount(2);
    }

    [Fact]
    public async Task ConversionPersistence_FailureShouldRollbackPlanAndSession()
    {
        var user = CreateUser();
        var plan = CreatePlan(user.Id);
        AddItem(plan, StudyPlanResourceType.KnowledgeNode, 15);
        plan.MarkReady(plan.Version, Now.AddHours(1));
        var duplicateSessionId = Guid.NewGuid();
        var existingSession = CreateSession(plan, duplicateSessionId);
        await using (var setup = fixture.CreateDbContext())
        {
            setup.Users.Add(user);
            setup.StudyPlans.Add(plan);
            setup.StudySessions.Add(existingSession);
            await setup.SaveChangesAsync();
        }

        using var provider = CreateServices();
        var persistence =
            provider.GetRequiredService<IStudyPlanConversionPersistence>();
        var stored = await persistence.GetPlanForUpdateAsync(
            user.Id, plan.Id, CancellationToken.None);
        var duplicate = CreateSession(stored!, duplicateSessionId);
        stored!.MarkConverted(
            stored.Version, duplicate.Id, Now.AddHours(2));
        persistence.AddStudySession(duplicate);
        var save = () => persistence.SaveChangesAsync(CancellationToken.None);

        await save.Should().ThrowAsync<StudyPlanConversionConflictException>();

        await using var verify = fixture.CreateDbContext();
        var unchanged = await verify.StudyPlans.AsNoTracking()
            .SingleAsync(x => x.Id == plan.Id);
        unchanged.Status.Should().Be(StudyPlanStatus.Ready);
        unchanged.ConvertedStudySessionId.Should().BeNull();
        (await verify.StudySessions.CountAsync(x =>
            x.Id == duplicateSessionId)).Should().Be(1);
    }

    [Fact]
    public async Task ConcurrentConversion_ShouldPersistExactlyOneSession()
    {
        var user = CreateUser();
        var plan = CreatePlan(user.Id);
        AddItem(plan, StudyPlanResourceType.DsaProblem, 30);
        plan.MarkReady(plan.Version, Now.AddHours(1));
        await using (var setup = fixture.CreateDbContext())
        {
            setup.Users.Add(user);
            setup.StudyPlans.Add(plan);
            await setup.SaveChangesAsync();
        }

        using var providerA = CreateServices();
        using var providerB = CreateServices();
        var persistenceA =
            providerA.GetRequiredService<IStudyPlanConversionPersistence>();
        var persistenceB =
            providerB.GetRequiredService<IStudyPlanConversionPersistence>();
        var planA = await persistenceA.GetPlanForUpdateAsync(
            user.Id, plan.Id, CancellationToken.None);
        var planB = await persistenceB.GetPlanForUpdateAsync(
            user.Id, plan.Id, CancellationToken.None);
        var sessionA = CreateSession(planA!, Guid.NewGuid());
        var sessionB = CreateSession(planB!, Guid.NewGuid());
        planA!.MarkConverted(planA.Version, sessionA.Id, Now.AddHours(2));
        planB!.MarkConverted(planB.Version, sessionB.Id, Now.AddHours(2));
        persistenceA.AddStudySession(sessionA);
        persistenceB.AddStudySession(sessionB);
        await persistenceA.SaveChangesAsync(CancellationToken.None);
        var secondSave = () =>
            persistenceB.SaveChangesAsync(CancellationToken.None);

        await secondSave.Should()
            .ThrowAsync<StudyPlanConversionConflictException>();

        await using var verify = fixture.CreateDbContext();
        var converted = await verify.StudyPlans.AsNoTracking()
            .SingleAsync(x => x.Id == plan.Id);
        converted.ConvertedStudySessionId.Should().Be(sessionA.Id);
        (await verify.StudySessions.CountAsync(x =>
            x.UserId == user.Id)).Should().Be(1);
    }

    [Fact]
    public async Task ReadyPlan_ShouldCancelAndRetainReadyTimestamp()
    {
        var user = CreateUser();
        var plan = CreatePlan(user.Id);
        AddItem(plan, StudyPlanResourceType.KnowledgeNode, 15);
        plan.MarkReady(plan.Version, Now.AddHours(1));
        await using (var setup = fixture.CreateDbContext())
        {
            setup.Users.Add(user);
            setup.StudyPlans.Add(plan);
            await setup.SaveChangesAsync();
        }

        using var provider = CreateServices();
        var repository = provider.GetRequiredService<IStudyPlanRepository>();
        var stored = await repository.GetByIdAndUserIdForUpdateAsync(
            plan.Id, user.Id, CancellationToken.None);
        stored!.Cancel(stored.Version, Now.AddHours(2));
        await repository.SaveChangesAsync(CancellationToken.None);

        await using var verify = fixture.CreateDbContext();
        var cancelled = await verify.StudyPlans.AsNoTracking()
            .SingleAsync(x => x.Id == plan.Id);
        cancelled.Status.Should().Be(StudyPlanStatus.Cancelled);
        cancelled.ReadyAtUtc.Should().Be(Now.AddHours(1));
        cancelled.CancelledAtUtc.Should().Be(Now.AddHours(2));
    }

    private ServiceProvider CreateServices()
    {
        var configuration = new ConfigurationManager();
        configuration["ConnectionStrings:Database"] = fixture.ConnectionString;
        return new ServiceCollection().AddInfrastructure(configuration)
            .BuildServiceProvider();
    }

    private static StudyPlan CreatePlan(Guid userId) =>
        StudyPlan.Create(
            Guid.NewGuid(), userId, "Evening Practice", Now,
            Now.Add(StudyPlanDefaults.DefaultLifetime));

    private static void AddItem(
        StudyPlan plan, StudyPlanResourceType resourceType, int minutes) =>
        plan.AddRecommendationItem(
            Guid.NewGuid(), Guid.NewGuid(), resourceType, Guid.NewGuid(),
            minutes, plan.UpdatedAtUtc.AddMinutes(1));

    private static StudySession CreateSession(StudyPlan plan, Guid sessionId)
    {
        var items = plan.Items.OrderBy(x => x.Position).Select(x =>
            new InitialStudySessionItem(
                Guid.NewGuid(),
                StudyPlanMappingPolicy.MapToStudyResourceType(x.ResourceType),
                x.ResourceId)).ToArray();
        return StudySession.CreateFromPlan(
            sessionId, plan.UserId, plan.Title,
            plan.TotalPlannedDurationMinutes, items, Now.AddHours(2));
    }

    private static StudyRecommendation CreateRecommendation(
        Guid userId, decimal score, DateTimeOffset generatedAt,
        DateTimeOffset? expiresAt)
    {
        var level = WeakTopicScoringPolicy.GetLevel(score);
        return StudyRecommendation.Create(
            Guid.NewGuid(), userId, RecommendationResourceType.DsaProblem,
            Guid.NewGuid(), RecommendationType.RetryDsaProblem,
            RecommendationPriorityPolicy.FromWeaknessLevel(level),
            RecommendationReason.Create(score, level, 3, generatedAt),
            generatedAt, expiresAt);
    }

    private static User CreateUser()
    {
        var suffix = Guid.NewGuid().ToString("N");
        return User.Create(
            Guid.NewGuid(), $"study-plan-{suffix}@example.com",
            "Study Plan User", "hash", Now);
    }
}
