using DevRecall.Domain.StudyPlans;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DevRecall.Infrastructure.Persistence.Configurations;

internal sealed class StudyPlanItemConfiguration
    : IEntityTypeConfiguration<StudyPlanItem>
{
    public void Configure(EntityTypeBuilder<StudyPlanItem> builder)
    {
        builder.ToTable("study_plan_items", table =>
        {
            table.HasCheckConstraint(
                "ck_study_plan_items_duration_range",
                $"planned_duration_minutes >= "
                + $"{StudyPlanDefaults.MinimumItemDurationMinutes} "
                + $"AND planned_duration_minutes <= "
                + $"{StudyPlanDefaults.MaximumItemDurationMinutes}");
            table.HasCheckConstraint(
                "ck_study_plan_items_position_positive", "position > 0");
            table.HasCheckConstraint(
                "ck_study_plan_items_source_reference",
                "(source_type = 1 AND source_recommendation_id IS NOT NULL) "
                + "OR source_type = 2");
        });
        builder.HasKey(item => item.Id);
        builder.Property(item => item.Id).ValueGeneratedNever();
        builder.Property(item => item.StudyPlanId).IsRequired();
        builder.Property(item => item.SourceRecommendationId);
        builder.Property(item => item.SourceType)
            .HasConversion<int>().IsRequired();
        builder.Property(item => item.ResourceType)
            .HasConversion<int>().IsRequired();
        builder.Property(item => item.ResourceId).IsRequired();
        builder.Property(item => item.PlannedDurationMinutes).IsRequired();
        builder.Property(item => item.Position).IsRequired();
        builder.Property(item => item.CreatedAtUtc).IsRequired();
        builder.Property(item => item.UpdatedAtUtc).IsRequired();
        builder.HasIndex(item => new { item.StudyPlanId, item.Position })
            .IsUnique()
            .HasDatabaseName("ux_study_plan_items_plan_position");
        builder.HasIndex(item => new
        {
            item.StudyPlanId,
            item.ResourceType,
            item.ResourceId
        }).IsUnique().HasDatabaseName("ux_study_plan_items_plan_resource");
        builder.HasIndex(item => item.SourceRecommendationId)
            .HasDatabaseName("ix_study_plan_items_source_recommendation");
    }
}
