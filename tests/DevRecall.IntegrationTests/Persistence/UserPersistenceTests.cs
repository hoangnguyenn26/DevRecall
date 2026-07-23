using DevRecall.Domain.Identity;
using DevRecall.IntegrationTests.Infrastructure;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace DevRecall.IntegrationTests.Persistence;

[Collection(PostgreSqlTestSuite.Name)]
public sealed class UserPersistenceTests(
    PostgreSqlFixture fixture)
{
    [Fact]
    public async Task ShouldPersistAndLoadUser()
    {
        await using var context = fixture.CreateDbContext();
        var user = User.Create(
            Guid.NewGuid(),
            $"user-{Guid.NewGuid():N}@example.com",
            "Hoang Nguyen",
            "test-password-hash",
            DateTimeOffset.UtcNow);

        context.Users.Add(user);

        await context.SaveChangesAsync(CancellationToken.None);

        context.ChangeTracker.Clear();

        var persisted = await context.Users.SingleAsync(
            item => item.Id == user.Id,
            CancellationToken.None);

        persisted.Email.Should().Be(user.Email);
        persisted.NormalizedEmail.Should().Be(user.NormalizedEmail);
        persisted.DisplayName.Should().Be(user.DisplayName);
        persisted.PasswordHash.Should().Be(user.PasswordHash);
        persisted.Status.Should().Be(UserStatus.Active);
    }

    [Fact]
    public async Task ShouldRejectDuplicateNormalizedEmail()
    {
        await using var context = fixture.CreateDbContext();
        var suffix = Guid.NewGuid().ToString("N");
        var firstUser = User.Create(
            Guid.NewGuid(),
            $"Hoang-{suffix}@example.com",
            "User One",
            "hash-1",
            DateTimeOffset.UtcNow);
        var secondUser = User.Create(
            Guid.NewGuid(),
            $"hoang-{suffix}@example.com",
            "User Two",
            "hash-2",
            DateTimeOffset.UtcNow);

        context.Users.AddRange(firstUser, secondUser);

        var action = async () =>
            await context.SaveChangesAsync(CancellationToken.None);

        await action.Should().ThrowAsync<DbUpdateException>();
    }
}
