using DevRecall.Domain.Identity;
using DevRecall.Domain.WeakTopics;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DevRecall.Infrastructure.Persistence.Configurations;

internal sealed class WeakTopicProfileConfiguration
    : IEntityTypeConfiguration<WeakTopicProfile>
{
    public void Configure(EntityTypeBuilder<WeakTopicProfile> builder)
    {
        builder.ToTable(
            "weak_topic_profiles",
            table =>
            {
                table.HasCheckConstraint(
                    "ck_weak_topic_profiles_score_non_negative",
                    "score >= 0");
                table.HasCheckConstraint(
                    "ck_weak_topic_profiles_signal_count_non_negative",
                    "signal_count >= 0");
            });
        builder.HasKey(profile => profile.Id);
        builder.Property(profile => profile.Id).ValueGeneratedNever();
        builder.Property(profile => profile.UserId).IsRequired();
        builder.Property(profile => profile.ResourceType)
            .HasConversion<int>()
            .IsRequired();
        builder.Property(profile => profile.ResourceId).IsRequired();
        builder.Property(profile => profile.Score)
            .HasPrecision(10, 2)
            .IsRequired();
        builder.Property(profile => profile.Level)
            .HasConversion<int>()
            .IsRequired();
        builder.Property(profile => profile.SignalCount).IsRequired();
        builder.Property(profile => profile.CalculatedAtUtc).IsRequired();
        builder.Property(profile => profile.CreatedAtUtc).IsRequired();
        builder.Property(profile => profile.UpdatedAtUtc).IsRequired();
        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(profile => profile.UserId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasIndex(profile => new
        {
            profile.UserId,
            profile.ResourceType,
            profile.ResourceId
        }).IsUnique()
            .HasDatabaseName("ux_weak_topic_profiles_user_resource");
        builder.HasIndex(profile => new
        {
            profile.UserId,
            profile.Level,
            profile.Score
        }).HasDatabaseName("ix_weak_topic_profiles_user_level_score");
        builder.HasIndex(profile => new
        {
            profile.UserId,
            profile.CalculatedAtUtc
        }).HasDatabaseName("ix_weak_topic_profiles_user_calculated_at");
    }
}
