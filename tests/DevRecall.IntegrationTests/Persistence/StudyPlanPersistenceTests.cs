using DevRecall.Application.StudyPlans;
using DevRecall.Application.StudyPlans.Generation;
using DevRecall.Domain.Identity;
using DevRecall.Domain.Recommendations;
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
