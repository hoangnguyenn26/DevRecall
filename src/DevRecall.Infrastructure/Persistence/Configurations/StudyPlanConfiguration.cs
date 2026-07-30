using DevRecall.Domain.Identity;
using DevRecall.Domain.StudyPlans;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DevRecall.Infrastructure.Persistence.Configurations;

internal sealed class StudyPlanConfiguration
    : IEntityTypeConfiguration<StudyPlan>
{
    public void Configure(EntityTypeBuilder<StudyPlan> builder)
    {
        builder.ToTable("study_plans", table =>
        {
            table.HasCheckConstraint(
                "ck_study_plans_version_positive", "version > 0");
            table.HasCheckConstraint(
                "ck_study_plans_expiration_after_generation",
                "expires_at_utc IS NULL OR expires_at_utc > generated_at_utc");
            table.HasCheckConstraint(
                "ck_study_plans_ready_state",
                "status NOT IN (2, 3) OR ready_at_utc IS NOT NULL");
            table.HasCheckConstraint(
                "ck_study_plans_converted_state",
                "(status = 3 AND converted_at_utc IS NOT NULL "
                + "AND converted_study_session_id IS NOT NULL) OR "
                + "(status <> 3 AND converted_at_utc IS NULL "
                + "AND converted_study_session_id IS NULL)");
            table.HasCheckConstraint(
                "ck_study_plans_cancelled_state",
                "(status = 4 AND cancelled_at_utc IS NOT NULL) OR "
                + "(status <> 4 AND cancelled_at_utc IS NULL)");
        });
        builder.HasKey(plan => plan.Id);
        builder.Property(plan => plan.Id).ValueGeneratedNever();
        builder.Property(plan => plan.UserId).IsRequired();
        builder.Property(plan => plan.Title)
            .HasMaxLength(StudyPlanDefaults.MaximumTitleLength).IsRequired();
        builder.Property(plan => plan.Status).HasConversion<int>().IsRequired();
        builder.Property(plan => plan.GeneratedAtUtc).IsRequired();
        builder.Property(plan => plan.ExpiresAtUtc);
        builder.Property(plan => plan.ReadyAtUtc);
        builder.Property(plan => plan.ConvertedAtUtc);
        builder.Property(plan => plan.ConvertedStudySessionId);
        builder.Property(plan => plan.CancelledAtUtc);
        builder.Property(plan => plan.CreatedAtUtc).IsRequired();
        builder.Property(plan => plan.UpdatedAtUtc).IsRequired();
        builder.Property(plan => plan.Version)
            .IsRequired().IsConcurrencyToken();
        builder.HasOne<User>().WithMany().HasForeignKey(plan => plan.UserId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasMany(plan => plan.Items).WithOne()
            .HasForeignKey(item => item.StudyPlanId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(plan => plan.Items)
            .UsePropertyAccessMode(PropertyAccessMode.Field);
        builder.HasIndex(plan => new
        {
            plan.UserId,
            plan.Status,
            plan.UpdatedAtUtc
        }).HasDatabaseName("ix_study_plans_user_status_updated_at");
        builder.HasIndex(plan => new { plan.UserId, plan.GeneratedAtUtc })
            .HasDatabaseName("ix_study_plans_user_generated_at");
        builder.HasIndex(plan => new { plan.UserId, plan.ExpiresAtUtc })
            .HasDatabaseName("ix_study_plans_user_expires_at");
        builder.HasIndex(plan => plan.UserId).IsUnique().HasFilter("status = 1")
            .HasDatabaseName("ux_study_plans_user_draft");
    }
}
