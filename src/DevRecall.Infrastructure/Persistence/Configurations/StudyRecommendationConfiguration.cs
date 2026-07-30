using DevRecall.Domain.Identity;
using DevRecall.Domain.Recommendations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DevRecall.Infrastructure.Persistence.Configurations;

internal sealed class StudyRecommendationConfiguration
    : IEntityTypeConfiguration<StudyRecommendation>
{
    public void Configure(EntityTypeBuilder<StudyRecommendation> builder)
    {
        builder.ToTable("study_recommendations", table =>
        {
            table.HasCheckConstraint(
                "ck_study_recommendations_priority_score_positive",
                "priority_score > 0");
            table.HasCheckConstraint(
                "ck_study_recommendations_signal_count_non_negative",
                "reason_signal_count >= 0");
            table.HasCheckConstraint(
                "ck_study_recommendations_version_positive", "version > 0");
        });
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.UserId).IsRequired();
        builder.Property(x => x.ResourceType).HasConversion<int>().IsRequired();
        builder.Property(x => x.ResourceId).IsRequired();
        builder.Property(x => x.Type).HasConversion<int>().IsRequired();
        builder.Property(x => x.Priority).HasConversion<int>().IsRequired();
        builder.Property(x => x.PriorityScore).HasPrecision(10, 2).IsRequired();
        builder.Property(x => x.Status).HasConversion<int>().IsRequired();
        builder.Property(x => x.GeneratedAtUtc).IsRequired();
        builder.Property(x => x.ExpiresAtUtc);
        builder.Property(x => x.DismissedAtUtc);
        builder.Property(x => x.CompletedAtUtc);
        builder.Property(x => x.ExpiredAtUtc);
        builder.Property(x => x.CreatedAtUtc).IsRequired();
        builder.Property(x => x.UpdatedAtUtc).IsRequired();
        builder.Property(x => x.Version).IsRequired().IsConcurrencyToken();
        builder.OwnsOne(x => x.Reason, owned =>
        {
            owned.Property(x => x.WeaknessScore)
                .HasColumnName("reason_weakness_score")
                .HasPrecision(10, 2).IsRequired();
            owned.Property(x => x.WeaknessLevel)
                .HasColumnName("reason_weakness_level")
                .HasConversion<int>().IsRequired();
            owned.Property(x => x.SignalCount)
                .HasColumnName("reason_signal_count").IsRequired();
            owned.Property(x => x.WeaknessCalculatedAtUtc)
                .HasColumnName("reason_weakness_calculated_at_utc").IsRequired();
        });
        builder.HasOne<User>().WithMany().HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasIndex(x => new
        {
            x.UserId,
            x.Status,
            x.Priority,
            x.PriorityScore
        }).HasDatabaseName(
            "ix_study_recommendations_user_status_priority_score");
        builder.HasIndex(x => new { x.UserId, x.GeneratedAtUtc })
            .HasDatabaseName("ix_study_recommendations_user_generated_at");
        builder.HasIndex(x => new { x.UserId, x.ExpiresAtUtc })
            .HasDatabaseName("ix_study_recommendations_user_expires_at");
        builder.HasIndex(x => new
        {
            x.UserId,
            x.ResourceType,
            x.ResourceId,
            x.Type
        }).IsUnique().HasFilter("status = 1")
            .HasDatabaseName(
                "ux_study_recommendations_active_user_resource_type");
    }
}
