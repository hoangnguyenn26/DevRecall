using System.Data.Common;
using DevRecall.Domain.Identity;
using DevRecall.Domain.Study;
using DevRecall.IntegrationTests.Infrastructure;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace DevRecall.IntegrationTests.Persistence;

[Collection(PostgreSqlTestSuite.Name)]
public sealed class StudySessionPersistenceTests(PostgreSqlFixture fixture)
{
    private static readonly DateTimeOffset Now =
        new(2026, 8, 20, 9, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task ShouldPersistSessionWithOrderedItems()
    {
        await using var context = fixture.CreateDbContext();
        var user = CreateUser();
        var session = CreateSession(user.Id);
        var first = session.AddItem(
            Guid.NewGuid(), StudyResourceType.KnowledgeNode,
            Guid.NewGuid(), "Read notes", Now);
        var second = session.AddItem(
            Guid.NewGuid(), StudyResourceType.DsaProblem,
            Guid.NewGuid(), null, Now);
        session.ReorderItems([second.Id, first.Id], Now.AddMinutes(1));
        context.Users.Add(user);
        context.StudySessions.Add(session);
        await context.SaveChangesAsync(CancellationToken.None);
        context.ChangeTracker.Clear();

        var persisted = await context.StudySessions
            .AsNoTracking()
            .Include(candidate => candidate.Items)
            .SingleAsync(
                candidate => candidate.Id == session.Id,
                CancellationToken.None);

        persisted.UserId.Should().Be(user.Id);
        persisted.Status.Should().Be(StudySessionStatus.Planned);
        persisted.Items.OrderBy(item => item.Position)
            .Select(item => item.Id)
            .Should().Equal(second.Id, first.Id);
    }

    [Fact]
    public async Task ShouldRejectDuplicateSessionResource()
    {
        await using var context = fixture.CreateDbContext();
        var user = CreateUser();
        var session = CreateSession(user.Id);
        var resourceId = Guid.NewGuid();
        session.AddItem(
            Guid.NewGuid(), StudyResourceType.ReviewItem,
            resourceId, null, Now);
        context.Users.Add(user);
        context.StudySessions.Add(session);
        await context.SaveChangesAsync(CancellationToken.None);

        var duplicateId = Guid.NewGuid();
        var action = async () => await context.Database.ExecuteSqlInterpolatedAsync(
            $"""
            INSERT INTO study_session_items
                (id, study_session_id, resource_type, resource_id, position,
                 status, notes, created_at_utc, updated_at_utc)
            VALUES
                ({duplicateId}, {session.Id}, {(int)StudyResourceType.ReviewItem},
                 {resourceId}, 1, {(int)StudySessionItemStatus.Pending},
                 NULL, {Now}, {Now})
            """,
            CancellationToken.None);

        await action.Should().ThrowAsync<DbException>();
    }

    [Fact]
    public async Task ShouldPersistCompletedLifecycle()
    {
        await using var context = fixture.CreateDbContext();
        var user = CreateUser();
        var session = CreateSession(user.Id);
        var item = session.AddItem(
            Guid.NewGuid(), StudyResourceType.InterviewQuestion,
            Guid.NewGuid(), null, Now);
        session.Start(Now.AddMinutes(1));
        session.CompleteItem(item.Id, "Finished", Now.AddMinutes(11));
        session.Complete(Now.AddMinutes(31));
        context.Users.Add(user);
        context.StudySessions.Add(session);
        await context.SaveChangesAsync(CancellationToken.None);
        context.ChangeTracker.Clear();

        var persisted = await context.StudySessions
            .AsNoTracking()
            .Include(candidate => candidate.Items)
            .SingleAsync(
                candidate => candidate.Id == session.Id,
                CancellationToken.None);

        persisted.Status.Should().Be(StudySessionStatus.Completed);
        persisted.ActualDurationMinutes.Should().Be(30);
        persisted.CompletedAtUtc.Should().Be(Now.AddMinutes(31));
        persisted.Items.Single().Status
            .Should().Be(StudySessionItemStatus.Completed);
        persisted.Items.Single().Notes.Should().Be("Finished");
    }

    [Fact]
    public async Task ShouldCascadeDeleteItemsWhenSessionIsDeleted()
    {
        await using var context = fixture.CreateDbContext();
        var user = CreateUser();
        var session = CreateSession(user.Id);
        var item = session.AddItem(
            Guid.NewGuid(), StudyResourceType.DsaProblem,
            Guid.NewGuid(), null, Now);
        context.Users.Add(user);
        context.StudySessions.Add(session);
        await context.SaveChangesAsync(CancellationToken.None);

        context.StudySessions.Remove(session);
        await context.SaveChangesAsync(CancellationToken.None);

        var itemExists = await context.StudySessionItems
            .AsNoTracking()
            .AnyAsync(
                candidate => candidate.Id == item.Id,
                CancellationToken.None);
        itemExists.Should().BeFalse();
    }

    private static StudySession CreateSession(Guid userId) =>
        StudySession.Create(
            Guid.NewGuid(), userId, "Evening study", 45, null, Now);

    private static User CreateUser()
    {
        var suffix = Guid.NewGuid().ToString("N");
        return User.Create(
            Guid.NewGuid(), $"study-{suffix}@example.com", "Study User",
            "test-password-hash", Now);
    }
}
