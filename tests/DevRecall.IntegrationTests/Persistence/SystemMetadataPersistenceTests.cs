using DevRecall.Domain.Common;
using DevRecall.IntegrationTests.Infrastructure;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace DevRecall.IntegrationTests.Persistence;

[Collection(PostgreSqlTestSuite.Name)]
public sealed class SystemMetadataPersistenceTests(
    PostgreSqlFixture fixture)
{
    [Fact]
    public async Task ShouldPersistAndLoadSystemMetadata()
    {
        await using var context = fixture.CreateDbContext();
        var key = $"application.version-{Guid.NewGuid():N}";
        var metadata = new SystemMetadata(
            Guid.NewGuid(),
            key,
            "0.1.0",
            DateTimeOffset.UtcNow);

        context.SystemMetadata.Add(metadata);

        await context.SaveChangesAsync(CancellationToken.None);

        context.ChangeTracker.Clear();

        var persisted = await context.SystemMetadata
            .SingleAsync(
                item => item.Id == metadata.Id,
                CancellationToken.None);

        persisted.Key.Should().Be(key);
        persisted.Value.Should().Be("0.1.0");
    }

    [Fact]
    public async Task ShouldRejectDuplicateMetadataKey()
    {
        await using var context = fixture.CreateDbContext();
        var key = $"duplicate-{Guid.NewGuid():N}";

        context.SystemMetadata.AddRange(
            new SystemMetadata(
                Guid.NewGuid(),
                key,
                "value-1",
                DateTimeOffset.UtcNow),
            new SystemMetadata(
                Guid.NewGuid(),
                key,
                "value-2",
                DateTimeOffset.UtcNow));

        var action = async () =>
            await context.SaveChangesAsync(CancellationToken.None);

        await action.Should().ThrowAsync<DbUpdateException>();
    }

    [Fact]
    public async Task FreshDatabaseShouldHaveEveryMigrationAppliedAndNoPendingMigrations()
    {
        await using var context = fixture.CreateDbContext();

        var defined = context.Database.GetMigrations().ToArray();
        var applied = await context.Database
            .GetAppliedMigrationsAsync(CancellationToken.None);
        var pending = await context.Database
            .GetPendingMigrationsAsync(CancellationToken.None);

        defined.Should().NotBeEmpty();
        applied.Should().Equal(defined);
        pending.Should().BeEmpty();
    }
}
