using DevRecall.Domain.Identity;
using DevRecall.Domain.Reviews;
using DevRecall.IntegrationTests.Infrastructure;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace DevRecall.IntegrationTests.Persistence;

[Collection(PostgreSqlTestSuite.Name)]
public sealed class ReviewPersistenceTests(PostgreSqlFixture fixture)
{
    private static readonly DateTimeOffset Now =
        new(2026, 8, 1, 9, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task ShouldPersistReviewItem()
    {
        await using var context = fixture.CreateDbContext();
        var user = CreateUser();
        var item = CreateItem(user.Id);
        context.Users.Add(user);
        context.ReviewItems.Add(item);
        await context.SaveChangesAsync(CancellationToken.None);
        context.ChangeTracker.Clear();

        var persisted = await context.ReviewItems
            .AsNoTracking()
            .SingleAsync(
                candidate => candidate.Id == item.Id,
                CancellationToken.None);

        persisted.UserId.Should().Be(user.Id);
        persisted.ResourceType.Should().Be(ReviewResourceType.DsaProblem);
        persisted.Status.Should().Be(ReviewItemStatus.Active);
        persisted.DueAtUtc.Should().Be(Now);
        persisted.IntervalDays.Should().Be(0);
    }

    [Fact]
    public async Task ShouldRejectDuplicateActiveReviewItem()
    {
        await using var context = fixture.CreateDbContext();
        var user = CreateUser();
        var resourceId = Guid.NewGuid();
        context.Users.Add(user);
        context.ReviewItems.AddRange(
            CreateItem(user.Id, resourceId),
            CreateItem(user.Id, resourceId));

        var action = async () =>
            await context.SaveChangesAsync(CancellationToken.None);

        await action.Should().ThrowAsync<DbUpdateException>();
    }

    [Fact]
    public async Task ShouldAllowReAddAfterArchive()
    {
        await using var context = fixture.CreateDbContext();
        var user = CreateUser();
        var resourceId = Guid.NewGuid();
        var archived = CreateItem(user.Id, resourceId);
        archived.Archive(Now.AddHours(1));
        context.Users.Add(user);
        context.ReviewItems.AddRange(
            archived, CreateItem(user.Id, resourceId));

        await context.SaveChangesAsync(CancellationToken.None);

        var count = await context.ReviewItems.CountAsync(
            item => item.UserId == user.Id
                && item.ResourceId == resourceId,
            CancellationToken.None);
        count.Should().Be(2);
    }

    [Fact]
    public async Task ShouldPersistEvaluationAndHistoryAtomically()
    {
        await using var context = fixture.CreateDbContext();
        var user = CreateUser();
        var item = CreateItem(user.Id);
        context.Users.Add(user);
        context.ReviewItems.Add(item);
        await context.SaveChangesAsync(CancellationToken.None);

        var reviewedAt = Now.AddHours(2);
        var schedule = item.Evaluate(ReviewEvaluation.Good, 0, reviewedAt);
        var history = ReviewHistory.Create(
            Guid.NewGuid(), item.Id, ReviewEvaluation.Good,
            schedule, reviewedAt);
        context.ReviewHistories.Add(history);
        await context.SaveChangesAsync(CancellationToken.None);
        context.ChangeTracker.Clear();

        var persistedItem = await context.ReviewItems
            .AsNoTracking()
            .SingleAsync(
                candidate => candidate.Id == item.Id,
                CancellationToken.None);
        var persistedHistory = await context.ReviewHistories
            .AsNoTracking()
            .SingleAsync(
                candidate => candidate.Id == history.Id,
                CancellationToken.None);

        persistedItem.ReviewCount.Should().Be(1);
        persistedItem.IntervalDays.Should().Be(2);
        persistedItem.DueAtUtc.Should().Be(reviewedAt.AddDays(2));
        persistedHistory.PreviousIntervalDays.Should().Be(0);
        persistedHistory.NextIntervalDays.Should().Be(2);
        persistedHistory.PreviousDueAtUtc.Should().Be(Now);
    }

    private static ReviewItem CreateItem(
        Guid userId, Guid? resourceId = null) =>
        ReviewItem.Create(
            Guid.NewGuid(), userId, ReviewResourceType.DsaProblem,
            resourceId ?? Guid.NewGuid(), Now, Now);

    private static User CreateUser()
    {
        var suffix = Guid.NewGuid().ToString("N");
        return User.Create(
            Guid.NewGuid(), $"review-{suffix}@example.com", "Review User",
            "test-password-hash", Now);
    }
}
