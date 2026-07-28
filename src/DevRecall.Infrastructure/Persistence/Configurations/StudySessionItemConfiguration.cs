using DevRecall.Domain.Study;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DevRecall.Infrastructure.Persistence.Configurations;

internal sealed class StudySessionItemConfiguration
    : IEntityTypeConfiguration<StudySessionItem>
{
    public void Configure(EntityTypeBuilder<StudySessionItem> builder)
    {
        builder.ToTable(
            "study_session_items",
            table => table.HasCheckConstraint(
                "ck_study_session_items_position_non_negative",
                "position >= 0"));
        builder.HasKey(item => item.Id);
        builder.Property(item => item.Id).ValueGeneratedNever();
        builder.Property(item => item.StudySessionId).IsRequired();
        builder.Property(item => item.ResourceType)
            .HasConversion<int>()
            .IsRequired();
        builder.Property(item => item.ResourceId).IsRequired();
        builder.Property(item => item.Position).IsRequired();
        builder.Property(item => item.Status)
            .HasConversion<int>()
            .IsRequired();
        builder.Property(item => item.StartedAtUtc);
        builder.Property(item => item.CompletedAtUtc);
        builder.Property(item => item.Notes).HasColumnType("text");
        builder.Property(item => item.CreatedAtUtc).IsRequired();
        builder.Property(item => item.UpdatedAtUtc).IsRequired();

        builder.HasIndex(item => new
        {
            item.StudySessionId,
            item.Position
        }).HasDatabaseName("ix_study_session_items_session_position");
        builder.HasIndex(item => new
        {
            item.StudySessionId,
            item.ResourceType,
            item.ResourceId
        })
            .IsUnique()
            .HasDatabaseName("ux_study_session_items_session_resource");
        builder.HasIndex(item => new
        {
            item.StudySessionId,
            item.Status
        }).HasDatabaseName("ix_study_session_items_session_status");
    }
}
