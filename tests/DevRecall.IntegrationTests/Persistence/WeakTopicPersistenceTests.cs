using DevRecall.Application.WeakTopics.GetList;
using DevRecall.Application.WeakTopics.Signals;
using DevRecall.Domain.Dsa;
using DevRecall.Domain.Dsa.Attempts;
using DevRecall.Domain.Identity;
using DevRecall.Domain.Reviews;
using DevRecall.Domain.Study;
using DevRecall.Domain.WeakTopics;
using DevRecall.Infrastructure;
using DevRecall.IntegrationTests.Infrastructure;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace DevRecall.IntegrationTests.Persistence;

[Collection(PostgreSqlTestSuite.Name)]
public sealed class WeakTopicPersistenceTests(PostgreSqlFixture fixture)
{
    private static readonly DateTimeOffset Now =
        new(2026, 9, 1, 0, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task ShouldPersistProfileWithDecimalPrecision()
    {
        await using var context = fixture.CreateDbContext();
        var user = CreateUser();
        var profile = CreateProfile(
            user.Id, Guid.NewGuid(), CreateBreakdown(7.35m));
        context.Users.Add(user);
        context.WeakTopicProfiles.Add(profile);
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        var persisted = await context.WeakTopicProfiles.AsNoTracking()
            .SingleAsync(item => item.Id == profile.Id);

        persisted.Score.Should().Be(7.35m);
        persisted.Level.Should().Be(WeaknessLevel.Medium);
        persisted.SignalCount.Should().Be(1);
    }

    [Fact]
    public async Task ShouldEnforceUniqueUserResourceProfile()
    {
        await using var context = fixture.CreateDbContext();
        var user = CreateUser();
        var resourceId = Guid.NewGuid();
        context.Users.Add(user);
        context.WeakTopicProfiles.AddRange(
            CreateProfile(user.Id, resourceId, CreateBreakdown(4m)),
            CreateProfile(user.Id, resourceId, CreateBreakdown(6m)));

        var action = async () => await context.SaveChangesAsync();

        await action.Should().ThrowAsync<DbUpdateException>();
    }

    [Fact]
    public async Task DeletingUser_ShouldCascadeProfiles()
    {
        var userId = Guid.NewGuid();
        await using (var setup = fixture.CreateDbContext())
        {
            var user = CreateUser(userId);
            setup.Users.Add(user);
            setup.WeakTopicProfiles.Add(CreateProfile(
                user.Id, Guid.NewGuid(), CreateBreakdown(4m)));
            await setup.SaveChangesAsync();
        }

        await using (var delete = fixture.CreateDbContext())
        {
            var user = await delete.Users.SingleAsync(item => item.Id == userId);
            delete.Users.Remove(user);
            await delete.SaveChangesAsync();
        }

        await using var verify = fixture.CreateDbContext();
        (await verify.WeakTopicProfiles.CountAsync(
            item => item.UserId == userId)).Should().Be(0);
    }

    [Fact]
    public async Task SignalReader_ShouldMapSourcesAndEnforceOwnershipAndRange()
    {
        var owner = CreateUser();
        var other = CreateUser();
        var problem = DsaProblem.Create(
            Guid.NewGuid(), owner.Id, "Signals", "Description",
            DsaProblemDifficulty.Easy, null, null, ["Array"], Now);
        var review = CreateReview(
            owner.Id, problem.Id, ReviewEvaluation.Again, Now.AddDays(1));
        var otherReview = CreateReview(
            other.Id, problem.Id, ReviewEvaluation.Easy, Now.AddDays(1));
        var study = CreateStudySession(
            owner.Id, problem.Id, StudySessionItemStatus.Skipped);
        var otherStudy = CreateStudySession(
            other.Id, problem.Id, StudySessionItemStatus.Completed);
        var attempt = CreateAttempt(
            problem.Id, 1, DsaAttemptResult.Failed, Now.AddDays(2));
        var excluded = CreateAttempt(
            problem.Id, 2, DsaAttemptResult.Solved, Now.AddDays(90));
        await using (var context = fixture.CreateDbContext())
        {
            context.Users.AddRange(owner, other);
            context.DsaProblems.Add(problem);
            context.ReviewItems.AddRange(review.Item, otherReview.Item);
            context.ReviewHistories.AddRange(
                review.History, otherReview.History);
            context.StudySessions.AddRange(study, otherStudy);
            context.DsaAttempts.AddRange(attempt, excluded);
            await context.SaveChangesAsync();
        }

        using var provider = CreateServices();
        var reader = provider.GetRequiredService<IWeakTopicSignalReader>();
        var result = await reader.ReadAsync(
            owner.Id, WeakTopicResourceType.DsaProblem, problem.Id,
            Now, Now.AddDays(90), CancellationToken.None);

        result.Select(item => item.SignalType).Should().Equal(
            WeaknessSignalType.ReviewAgain,
            WeaknessSignalType.StudyItemSkipped,
            WeaknessSignalType.DsaFailed);
        result.Should().BeInAscendingOrder(item => item.OccurredAtUtc);
    }

    [Fact]
    public async Task ListReader_ShouldFilterOwnProfilesOrderAndPaginate()
    {
        var owner = CreateUser();
        var other = CreateUser();
        var firstId = Guid.Parse("00000000-0000-0000-0000-000000000001");
        var secondId = Guid.Parse("00000000-0000-0000-0000-000000000002");
        await using (var context = fixture.CreateDbContext())
        {
            context.Users.AddRange(owner, other);
            context.WeakTopicProfiles.AddRange(
                CreateProfile(owner.Id, Guid.NewGuid(), CreateBreakdown(0m)),
                CreateProfile(
                    owner.Id, Guid.NewGuid(), CreateBreakdown(9m, Now.AddDays(1)),
                    firstId),
                CreateProfile(
                    owner.Id, Guid.NewGuid(), CreateBreakdown(9m, Now.AddDays(2)),
                    secondId),
                CreateProfile(owner.Id, Guid.NewGuid(), CreateBreakdown(4m)),
                CreateProfile(other.Id, Guid.NewGuid(), CreateBreakdown(20m)));
            await context.SaveChangesAsync();
        }

        using var provider = CreateServices();
        var reader = provider.GetRequiredService<IWeakTopicListReader>();
        var defaultPage = await reader.ReadAsync(
            owner.Id, null, null, null, false, 0, 10, CancellationToken.None);
        var nonePage = await reader.ReadAsync(
            owner.Id, WeaknessLevel.None, null, null, false, 0, 10,
            CancellationToken.None);
        var paged = await reader.ReadAsync(
            owner.Id, null, null, 4m, false, 1, 1, CancellationToken.None);

        defaultPage.TotalCount.Should().Be(3);
        defaultPage.Items.Select(x => x.ProfileId).Should().Equal(secondId, firstId,
            defaultPage.Items[2].ProfileId);
        nonePage.Items.Should().ContainSingle(x => x.Level == WeaknessLevel.None);
        paged.TotalCount.Should().Be(3);
        paged.Items.Should().ContainSingle(x => x.ProfileId == firstId);
    }

    private ServiceProvider CreateServices()
    {
        var configuration = new ConfigurationManager();
        configuration["ConnectionStrings:Database"] =
            fixture.ConnectionString;
        return new ServiceCollection()
            .AddInfrastructure(configuration)
            .BuildServiceProvider();
    }

    private static WeakTopicProfile CreateProfile(
        Guid userId, Guid resourceId, WeaknessScoreBreakdown breakdown,
        Guid? profileId = null) =>
        WeakTopicProfile.Create(
            profileId ?? Guid.NewGuid(), userId, WeakTopicResourceType.KnowledgeNode,
            resourceId, breakdown, breakdown.CalculatedAtUtc);

    private static WeaknessScoreBreakdown CreateBreakdown(
        decimal score, DateTimeOffset? calculatedAt = null) =>
        new(
            score, score, WeakTopicScoringPolicy.GetLevel(score), 1,
            calculatedAt ?? Now,
            [new WeaknessSignalContribution(
                WeaknessSignalType.ReviewAgain, 4, 1m, score)]);

    private static (ReviewItem Item, ReviewHistory History) CreateReview(
        Guid userId, Guid resourceId, ReviewEvaluation evaluation,
        DateTimeOffset reviewedAt)
    {
        var item = ReviewItem.Create(
            Guid.NewGuid(), userId, ReviewResourceType.DsaProblem,
            resourceId, reviewedAt, reviewedAt.AddDays(-1));
        var schedule = item.Evaluate(evaluation, 0, reviewedAt);
        return (item, ReviewHistory.Create(
            Guid.NewGuid(), item.Id, evaluation, schedule, reviewedAt));
    }

    private static StudySession CreateStudySession(
        Guid userId, Guid resourceId, StudySessionItemStatus status)
    {
        var session = StudySession.Create(
            Guid.NewGuid(), userId, "Signals", 30, null, Now);
        var item = session.AddItem(
            Guid.NewGuid(), StudyResourceType.DsaProblem,
            resourceId, null, Now);
        session.Start(Now);
        if (status == StudySessionItemStatus.Completed)
        {
            session.CompleteItem(item.Id, null, Now.AddDays(1));
        }
        else
        {
            session.SkipItem(item.Id, null, Now.AddDays(1));
        }

        return session;
    }

    private static DsaAttempt CreateAttempt(
        Guid problemId, int number, DsaAttemptResult result,
        DateTimeOffset attemptedAt) =>
        DsaAttempt.Create(
            Guid.NewGuid(), problemId, number, result, "C#", null, null,
            null, null, 10, null, attemptedAt, attemptedAt);

    private static User CreateUser(Guid? id = null)
    {
        var suffix = Guid.NewGuid().ToString("N");
        return User.Create(
            id ?? Guid.NewGuid(), $"weak-{suffix}@example.com", "Weak User",
            "hash", Now);
    }
}
