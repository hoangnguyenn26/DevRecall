using DevRecall.Domain.Reviews;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DevRecall.Infrastructure.Persistence.Configurations;

internal sealed class ReviewHistoryConfiguration
    : IEntityTypeConfiguration<ReviewHistory>
{
    public void Configure(EntityTypeBuilder<ReviewHistory> builder)
    {
        builder.ToTable(
            "review_histories",
            table =>
            {
                table.HasCheckConstraint(
                    "ck_review_histories_previous_interval_non_negative",
                    "previous_interval_days >= 0");
                table.HasCheckConstraint(
                    "ck_review_histories_next_interval_positive",
                    "next_interval_days > 0");
            });
        builder.HasKey(history => history.Id);
        builder.Property(history => history.Id).ValueGeneratedNever();
        builder.Property(history => history.ReviewItemId).IsRequired();
        builder.Property(history => history.Evaluation)
            .HasConversion<int>()
            .IsRequired();
        builder.Property(history => history.PreviousIntervalDays).IsRequired();
        builder.Property(history => history.NextIntervalDays).IsRequired();
        builder.Property(history => history.PreviousDueAtUtc).IsRequired();
        builder.Property(history => history.NextDueAtUtc).IsRequired();
        builder.Property(history => history.ReviewedAtUtc).IsRequired();
        builder.Property(history => history.CreatedAtUtc).IsRequired();

        builder.HasOne<ReviewItem>()
            .WithMany()
            .HasForeignKey(history => history.ReviewItemId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(history => new
        {
            history.ReviewItemId,
            history.ReviewedAtUtc
        }).HasDatabaseName("ix_review_histories_item_reviewed_at");
    }
}
