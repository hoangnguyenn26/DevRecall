using DevRecall.Domain.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DevRecall.Infrastructure.Persistence.Configurations;

internal sealed class SystemMetadataConfiguration
    : IEntityTypeConfiguration<SystemMetadata>
{
    public void Configure(EntityTypeBuilder<SystemMetadata> builder)
    {
        builder.ToTable("system_metadata");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .ValueGeneratedNever();

        builder.Property(x => x.Key)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(x => x.Value)
            .HasMaxLength(2000)
            .IsRequired();

        builder.Property(x => x.CreatedAtUtc)
            .IsRequired();

        builder.HasIndex(x => x.Key)
            .IsUnique()
            .HasDatabaseName("ux_system_metadata_key");
    }
}
