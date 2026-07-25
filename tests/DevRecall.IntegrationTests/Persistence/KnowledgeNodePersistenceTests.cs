using DevRecall.Domain.Identity;
using DevRecall.Domain.Knowledge;
using DevRecall.IntegrationTests.Infrastructure;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace DevRecall.IntegrationTests.Persistence;

[Collection(PostgreSqlTestSuite.Name)]
public sealed class KnowledgeNodePersistenceTests(PostgreSqlFixture fixture)
{
    [Fact]
    public async Task ShouldPersistRootKnowledgeNode()
    {
        await using var context = fixture.CreateDbContext();
        var user = CreateUser();
        var node = KnowledgeNode.Create(
            Guid.NewGuid(),
            user.Id,
            null,
            "Programming",
            0,
            DateTimeOffset.UtcNow);

        context.Users.Add(user);
        context.KnowledgeNodes.Add(node);

        await context.SaveChangesAsync(CancellationToken.None);

        context.ChangeTracker.Clear();

        var persisted = await context.KnowledgeNodes.SingleAsync(
            item => item.Id == node.Id,
            CancellationToken.None);

        persisted.ParentId.Should().BeNull();
        persisted.UserId.Should().Be(user.Id);
        persisted.Title.Should().Be("Programming");
    }

    [Fact]
    public async Task ShouldPersistChildWithParent()
    {
        await using var context = fixture.CreateDbContext();
        var user = CreateUser();
        var parent = CreateNode(user.Id, title: "Programming");
        var child = CreateNode(user.Id, parent.Id, "C#");

        context.Users.Add(user);
        context.KnowledgeNodes.AddRange(parent, child);

        await context.SaveChangesAsync(CancellationToken.None);

        context.ChangeTracker.Clear();

        var persisted = await context.KnowledgeNodes.SingleAsync(
            item => item.Id == child.Id,
            CancellationToken.None);

        persisted.ParentId.Should().Be(parent.Id);
    }

    [Fact]
    public async Task ShouldRejectNonExistingParent()
    {
        await using var context = fixture.CreateDbContext();
        var user = CreateUser();
        var node = CreateNode(
            user.Id,
            Guid.NewGuid(),
            "Invalid child");

        context.Users.Add(user);
        context.KnowledgeNodes.Add(node);

        var action = () =>
            context.SaveChangesAsync(CancellationToken.None);

        await action.Should().ThrowAsync<DbUpdateException>();
    }

    [Fact]
    public async Task ShouldRejectNonExistingOwner()
    {
        await using var context = fixture.CreateDbContext();
        var node = CreateNode(Guid.NewGuid(), title: "Invalid owner");

        context.KnowledgeNodes.Add(node);

        var action = () =>
            context.SaveChangesAsync(CancellationToken.None);

        await action.Should().ThrowAsync<DbUpdateException>();
    }

    [Fact]
    public async Task ShouldRestrictDeletingParentWithChildren()
    {
        await using var context = fixture.CreateDbContext();
        var user = CreateUser();
        var parent = CreateNode(user.Id, title: "Parent");
        var child = CreateNode(user.Id, parent.Id, "Child");

        context.Users.Add(user);
        context.KnowledgeNodes.AddRange(parent, child);

        await context.SaveChangesAsync(CancellationToken.None);

        context.ChangeTracker.Clear();

        var loadedParent = await context.KnowledgeNodes.SingleAsync(
            item => item.Id == parent.Id,
            CancellationToken.None);
        context.KnowledgeNodes.Remove(loadedParent);

        var action = () =>
            context.SaveChangesAsync(CancellationToken.None);

        await action.Should().ThrowAsync<DbUpdateException>();
    }

    private static User CreateUser()
    {
        return User.Create(
            Guid.NewGuid(),
            $"user-{Guid.NewGuid():N}@example.com",
            "Test User",
            "test-password-hash",
            DateTimeOffset.UtcNow);
    }

    private static KnowledgeNode CreateNode(
        Guid userId,
        Guid? parentId = null,
        string title = "Node")
    {
        return KnowledgeNode.Create(
            Guid.NewGuid(),
            userId,
            parentId,
            title,
            0,
            DateTimeOffset.UtcNow);
    }
}
