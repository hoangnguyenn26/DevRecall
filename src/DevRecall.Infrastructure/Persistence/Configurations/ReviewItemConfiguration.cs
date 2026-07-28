using DevRecall.Domain.Identity;
using DevRecall.Domain.Reviews;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DevRecall.Infrastructure.Persistence.Configurations;

internal sealed class ReviewItemConfiguration
    : IEntityTypeConfiguration<ReviewItem>
{
    public void Configure(EntityTypeBuilder<ReviewItem> builder)
    {
        builder.ToTable(
            "review_items",
            table =>
            {
                table.HasCheckConstraint(
                    "ck_review_items_interval_days_non_negative",
                    "interval_days >= 0");
                table.HasCheckConstraint(
                    "ck_review_items_review_count_non_negative",
                    "review_count >= 0");
            });
        builder.HasKey(item => item.Id);
        builder.Property(item => item.Id).ValueGeneratedNever();
        builder.Property(item => item.UserId).IsRequired();
        builder.Property(item => item.ResourceType).HasConversion<int>().IsRequired();
        builder.Property(item => item.ResourceId).IsRequired();
        builder.Property(item => item.Status).HasConversion<int>().IsRequired();
        builder.Property(item => item.DueAtUtc).IsRequired();
        builder.Property(item => item.LastReviewedAtUtc);
        builder.Property(item => item.IntervalDays).IsRequired();
        builder.Property(item => item.ReviewCount).IsRequired();
        builder.Property(item => item.CreatedAtUtc).IsRequired();
        builder.Property(item => item.UpdatedAtUtc).IsRequired();

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(item => item.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(item => new
        {
            item.UserId,
            item.Status,
            item.DueAtUtc
        }).HasDatabaseName("ix_review_items_user_status_due_at");

        builder.HasIndex(item => new
        {
            item.UserId,
            item.ResourceType,
            item.Status
        }).HasDatabaseName("ix_review_items_user_resource_type_status");

        builder.HasIndex(item => new
        {
            item.UserId,
            item.ResourceType,
            item.ResourceId
        })
            .IsUnique()
            .HasFilter("status = 1")
            .HasDatabaseName("ux_review_items_active_resource");
    }
}
