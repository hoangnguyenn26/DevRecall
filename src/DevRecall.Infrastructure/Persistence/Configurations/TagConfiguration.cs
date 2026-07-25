using DevRecall.Domain.Identity;
using DevRecall.Domain.Knowledge.Tags;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DevRecall.Infrastructure.Persistence.Configurations;

internal sealed class TagConfiguration : IEntityTypeConfiguration<Tag>
{
    public void Configure(EntityTypeBuilder<Tag> builder)
    {
        builder.ToTable("tags");
        builder.HasKey(tag => tag.Id);

        builder.Property(tag => tag.Id).ValueGeneratedNever();
        builder.Property(tag => tag.UserId).IsRequired();
        builder.Property(tag => tag.Name).HasMaxLength(TagName.MaxLength).IsRequired();
        builder.Property(tag => tag.NormalizedName)
            .HasMaxLength(TagName.MaxLength)
            .IsRequired();
        builder.Property(tag => tag.Status).HasConversion<int>().IsRequired();
        builder.Property(tag => tag.CreatedAtUtc).IsRequired();
        builder.Property(tag => tag.UpdatedAtUtc).IsRequired();

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(tag => tag.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(tag => new { tag.UserId, tag.NormalizedName })
            .IsUnique()
            .HasDatabaseName("ux_tags_user_id_normalized_name");

        builder.HasIndex(tag => new { tag.UserId, tag.Status, tag.Name })
            .HasDatabaseName("ix_tags_user_status_name");
    }
}
