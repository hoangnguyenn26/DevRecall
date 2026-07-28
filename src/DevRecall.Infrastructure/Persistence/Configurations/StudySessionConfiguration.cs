using DevRecall.Domain.Identity;
using DevRecall.Domain.Study;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DevRecall.Infrastructure.Persistence.Configurations;

internal sealed class StudySessionConfiguration
    : IEntityTypeConfiguration<StudySession>
{
    public void Configure(EntityTypeBuilder<StudySession> builder)
    {
        builder.ToTable(
            "study_sessions",
            table =>
            {
                table.HasCheckConstraint(
                    "ck_study_sessions_planned_duration_non_negative",
                    "planned_duration_minutes >= 0");
                table.HasCheckConstraint(
                    "ck_study_sessions_actual_duration_non_negative",
                    "actual_duration_minutes IS NULL OR actual_duration_minutes >= 0");
            });
        builder.HasKey(session => session.Id);
        builder.Property(session => session.Id).ValueGeneratedNever();
        builder.Property(session => session.UserId).IsRequired();
        builder.Property(session => session.Title)
            .HasMaxLength(StudySessionText.TitleMaxLength)
            .IsRequired();
        builder.Property(session => session.Status)
            .HasConversion<int>()
            .IsRequired();
        builder.Property(session => session.PlannedDurationMinutes).IsRequired();
        builder.Property(session => session.StartedAtUtc);
        builder.Property(session => session.CompletedAtUtc);
        builder.Property(session => session.ActualDurationMinutes);
        builder.Property(session => session.Notes).HasColumnType("text");
        builder.Property(session => session.CreatedAtUtc).IsRequired();
        builder.Property(session => session.UpdatedAtUtc).IsRequired();

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(session => session.UserId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasMany(session => session.Items)
            .WithOne()
            .HasForeignKey(item => item.StudySessionId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(session => session.Items)
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasIndex(session => new
        {
            session.UserId,
            session.Status,
            session.CreatedAtUtc
        }).HasDatabaseName("ix_study_sessions_user_status_created_at");
        builder.HasIndex(session => new
        {
            session.UserId,
            session.CompletedAtUtc
        }).HasDatabaseName("ix_study_sessions_user_completed_at");
    }
}
