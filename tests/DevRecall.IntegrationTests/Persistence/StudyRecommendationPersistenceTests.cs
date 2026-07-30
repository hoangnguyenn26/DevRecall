using DevRecall.Application.Recommendations;
using DevRecall.Application.Recommendations.Generation;
using DevRecall.Application.Recommendations.GetList;
using DevRecall.Domain.Identity;
using DevRecall.Domain.Recommendations;
using DevRecall.Domain.WeakTopics;
using DevRecall.Infrastructure;
using DevRecall.IntegrationTests.Infrastructure;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace DevRecall.IntegrationTests.Persistence;

[Collection(PostgreSqlTestSuite.Name)]
public sealed class StudyRecommendationPersistenceTests(PostgreSqlFixture fixture)
{
    private static readonly DateTimeOffset Now =
        new(2026, 9, 8, 10, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task ShouldPersistRecommendationAndReasonWithPrecision()
    {
        await using var context = fixture.CreateDbContext();
        var user = CreateUser();
        var recommendation = CreateRecommendation(user.Id, 11.75m);
        context.Users.Add(user);
        context.StudyRecommendations.Add(recommendation);
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        var stored = await context.StudyRecommendations.AsNoTracking()
            .SingleAsync(x => x.Id == recommendation.Id);

        stored.PriorityScore.Should().Be(11.75m);
        stored.Reason.WeaknessScore.Should().Be(11.75m);
        stored.Reason.WeaknessLevel.Should().Be(WeaknessLevel.High);
        stored.Status.Should().Be(RecommendationStatus.Active);
        stored.Version.Should().Be(1);
    }

    [Fact]
    public async Task PartialUniqueIndex_ShouldAllowHistoryButOnlyOneActive()
    {
        await using var context = fixture.CreateDbContext();
        var user = CreateUser();
        var resourceId = Guid.NewGuid();
        var first = CreateRecommendation(user.Id, 8m, resourceId);
        context.Users.Add(user);
        context.StudyRecommendations.Add(first);
        await context.SaveChangesAsync();
        context.StudyRecommendations.Add(
            CreateRecommendation(user.Id, 9m, resourceId));

        var duplicate = async () => await context.SaveChangesAsync();

        await duplicate.Should().ThrowAsync<DbUpdateException>();
        context.ChangeTracker.Clear();
        first = await context.StudyRecommendations.SingleAsync(x => x.Id == first.Id);
        first.Dismiss(Now.AddDays(1));
        await context.SaveChangesAsync();
        context.StudyRecommendations.Add(
            CreateRecommendation(user.Id, 9m, resourceId, Now.AddDays(2)));
        await context.SaveChangesAsync();
    }

    [Fact]
    public async Task Repository_ShouldMapOptimisticConcurrencyConflict()
    {
        var user = CreateUser();
        var recommendation = CreateRecommendation(user.Id, 8m);
        await using (var setup = fixture.CreateDbContext())
        {
            setup.Users.Add(user);
            setup.StudyRecommendations.Add(recommendation);
            await setup.SaveChangesAsync();
        }

        using var providerA = CreateServices();
        using var providerB = CreateServices();
        var repositoryA = providerA.GetRequiredService<IStudyRecommendationRepository>();
        var repositoryB = providerB.GetRequiredService<IStudyRecommendationRepository>();
        var itemA = await repositoryA.GetByIdAndUserIdForUpdateAsync(
            recommendation.Id, user.Id, CancellationToken.None);
        var itemB = await repositoryB.GetByIdAndUserIdForUpdateAsync(
            recommendation.Id, user.Id, CancellationToken.None);
        itemA!.Dismiss(Now.AddDays(1));
        await repositoryA.SaveChangesAsync(CancellationToken.None);
        itemB!.Complete(Now.AddDays(1));

        var conflict = () => repositoryB.SaveChangesAsync(CancellationToken.None);

        await conflict.Should().ThrowAsync<RecommendationPersistenceConflictException>();
    }

    [Fact]
    public async Task CandidateReader_ShouldScopeFilterOrderAndBoundTake()
    {
        await using var context = fixture.CreateDbContext();
        var owner = CreateUser();
        var other = CreateUser();
        context.Users.AddRange(owner, other);
        context.WeakTopicProfiles.AddRange(
            CreateProfile(owner.Id, 8.5m, Now.AddDays(-2)),
            CreateProfile(owner.Id, 9m, Now.AddDays(-5)),
            CreateProfile(owner.Id, 8.5m, Now.AddDays(-1)),
            CreateProfile(owner.Id, 0m, Now),
            CreateProfile(other.Id, 20m, Now));
        await context.SaveChangesAsync();

        using var provider = CreateServices();
        var reader = provider.GetRequiredService<IRecommendationCandidateReader>();
        var candidates = await reader.ReadAsync(
            owner.Id, 3, CancellationToken.None);

        candidates.Select(x => x.WeaknessScore).Should().Equal(9m, 8.5m, 8.5m);
        candidates[1].WeaknessCalculatedAtUtc.Should().Be(Now.AddDays(-1));
        var invalid = () => reader.ReadAsync(owner.Id, 501, CancellationToken.None);
        await invalid.Should().ThrowAsync<ArgumentOutOfRangeException>();
    }

    [Fact]
    public async Task ListReader_ShouldScopeFilterOrderAndPaginate()
    {
        var owner = CreateUser();
        var other = CreateUser();
        var critical = CreateRecommendation(
            owner.Id, 12m, generatedAt: Now.AddDays(-2));
        var highNewer = CreateRecommendation(
            owner.Id, 9m, generatedAt: Now.AddDays(-1));
        var highOlder = CreateRecommendation(
            owner.Id, 9m, generatedAt: Now.AddDays(-3));
        var completed = CreateRecommendation(owner.Id, 8m);
        completed.Complete(Now.AddDays(1));
        await using (var context = fixture.CreateDbContext())
        {
            context.Users.AddRange(owner, other);
            context.StudyRecommendations.AddRange(
                critical, highNewer, highOlder, completed,
                CreateRecommendation(other.Id, 20m));
            await context.SaveChangesAsync();
        }

        using var provider = CreateServices();
        var reader = provider.GetRequiredService<IRecommendationListReader>();
        var active = await reader.ReadAsync(
            owner.Id, RecommendationStatus.Active, null, null, null, null,
            0, 10, CancellationToken.None);
        var history = await reader.ReadAsync(
            owner.Id, RecommendationStatus.Completed, null, null, null, null,
            0, 10, CancellationToken.None);
        var paged = await reader.ReadAsync(
            owner.Id, RecommendationStatus.Active, null, null, null, 9m,
            1, 1, CancellationToken.None);

        active.Items.Select(x => x.RecommendationId).Should()
            .Equal(critical.Id, highNewer.Id, highOlder.Id);
        history.Items.Should().ContainSingle(x =>
            x.RecommendationId == completed.Id);
        paged.TotalCount.Should().Be(3);
        paged.Items.Should().ContainSingle(x =>
            x.RecommendationId == highNewer.Id);
    }

    private ServiceProvider CreateServices()
    {
        var configuration = new ConfigurationManager();
        configuration["ConnectionStrings:Database"] = fixture.ConnectionString;
        return new ServiceCollection().AddInfrastructure(configuration)
            .BuildServiceProvider();
    }

    private static StudyRecommendation CreateRecommendation(
        Guid userId, decimal score, Guid? resourceId = null,
        DateTimeOffset? generatedAt = null)
    {
        var at = generatedAt ?? Now;
        var level = WeakTopicScoringPolicy.GetLevel(score);
        return StudyRecommendation.Create(
            Guid.NewGuid(), userId, RecommendationResourceType.DsaProblem,
            resourceId ?? Guid.NewGuid(), RecommendationType.RetryDsaProblem,
            RecommendationPriorityPolicy.FromWeaknessLevel(level),
            RecommendationReason.Create(score, level, 3, at),
            at, at.Add(RecommendationDefaults.ActiveLifetime));
    }

    private static WeakTopicProfile CreateProfile(
        Guid userId, decimal score, DateTimeOffset calculatedAt) =>
        WeakTopicProfile.Create(
            Guid.NewGuid(), userId, WeakTopicResourceType.KnowledgeNode,
            Guid.NewGuid(), new(
                score, score, WeakTopicScoringPolicy.GetLevel(score),
                score == 0 ? 0 : 1, calculatedAt, []), calculatedAt);

    private static User CreateUser()
    {
        var suffix = Guid.NewGuid().ToString("N");
        return User.Create(
            Guid.NewGuid(), $"recommendation-{suffix}@example.com",
            "Recommendation User", "hash", Now);
    }
}
