using DevRecall.Domain.Identity;
using DevRecall.Domain.Knowledge.Tags;
using DevRecall.IntegrationTests.Infrastructure;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace DevRecall.IntegrationTests.Persistence;

[Collection(PostgreSqlTestSuite.Name)]
public sealed class TagPersistenceTests(PostgreSqlFixture fixture)
{
    [Fact]
    public async Task ShouldRejectDuplicateNormalizedNameForSameUser()
    {
        await using var context = fixture.CreateDbContext();
        var user = CreateUser();
        var now = DateTimeOffset.UtcNow;

        context.Users.Add(user);
        context.Tags.AddRange(
            Tag.Create(Guid.NewGuid(), user.Id, "Interview", now),
            Tag.Create(Guid.NewGuid(), user.Id, " interview ", now));

        var action = async () => await context.SaveChangesAsync(CancellationToken.None);

        await action.Should().ThrowAsync<DbUpdateException>();
    }

    [Fact]
    public async Task ShouldAllowSameNormalizedNameForDifferentUsers()
    {
        await using var context = fixture.CreateDbContext();
        var firstUser = CreateUser();
        var secondUser = CreateUser();
        var now = DateTimeOffset.UtcNow;

        context.Users.AddRange(firstUser, secondUser);
        context.Tags.AddRange(
            Tag.Create(Guid.NewGuid(), firstUser.Id, "Interview", now),
            Tag.Create(Guid.NewGuid(), secondUser.Id, " interview ", now));

        var action = async () => await context.SaveChangesAsync(CancellationToken.None);

        await action.Should().NotThrowAsync();
    }

    private static User CreateUser()
    {
        var suffix = Guid.NewGuid().ToString("N");
        return User.Create(
            Guid.NewGuid(),
            $"user-{suffix}@example.com",
            "Test User",
            "test-password-hash",
            DateTimeOffset.UtcNow);
    }
}
