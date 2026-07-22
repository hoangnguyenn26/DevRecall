using DevRecall.Domain.Common;
using DevRecall.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace DevRecall.IntegrationTests.Persistence;

public sealed class DevRecallDbContextModelTests
{
    [Fact]
    public void Model_SystemMetadata_HasExpectedPostgreSqlMapping()
    {
        var options = new DbContextOptionsBuilder<DevRecallDbContext>()
            .UseNpgsql(
                "Host=localhost;Database=dummy;Username=dummy;Password=dummy")
            .UseSnakeCaseNamingConvention()
            .Options;

        using var context = new DevRecallDbContext(options);

        var entityType = context.Model.FindEntityType(typeof(SystemMetadata));
        entityType.Should().NotBeNull();
        entityType!.GetTableName().Should().Be("system_metadata");

        var table = StoreObjectIdentifier.Table("system_metadata", schema: null);
        entityType.FindProperty(nameof(SystemMetadata.Id))!
            .GetColumnName(table).Should().Be("id");
        entityType.FindProperty(nameof(SystemMetadata.Id))!
            .ValueGenerated.Should().Be(ValueGenerated.Never);
        entityType.FindProperty(nameof(SystemMetadata.CreatedAtUtc))!
            .GetColumnName(table).Should().Be("created_at_utc");
        entityType.FindProperty(nameof(SystemMetadata.CreatedAtUtc))!
            .GetColumnType().Should().Be("timestamp with time zone");

        var keyIndex = entityType.GetIndexes().Single();
        keyIndex.IsUnique.Should().BeTrue();
        keyIndex.GetDatabaseName().Should().Be("ux_system_metadata_key");
    }
}
