using DevRecall.Domain.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DevRecall.Infrastructure.Persistence.Configurations;

internal sealed class UserLearningPreferenceConfiguration
    : IEntityTypeConfiguration<UserLearningPreference>
{
    public void Configure(EntityTypeBuilder<UserLearningPreference> builder)
    {
        builder.ToTable("user_learning_preferences", table =>
        {
            table.HasCheckConstraint("ck_user_learning_preferences_daily_minutes",
                "daily_commitment_minutes BETWEEN 10 AND 180");
            table.HasCheckConstraint("ck_user_learning_preferences_weekly_days",
                "weekly_target_days BETWEEN 1 AND 7");
            table.HasCheckConstraint("ck_user_learning_preferences_goal",
                "goal BETWEEN 1 AND 4");
            table.HasCheckConstraint("ck_user_learning_preferences_completion_type",
                "completion_type IN (1, 2)");
            table.HasCheckConstraint("ck_user_learning_preferences_version",
                "version > 0");
        });
        builder.HasKey(item => item.UserId);
        builder.Property(item => item.UserId).ValueGeneratedNever();
        builder.Property(item => item.Goal).HasConversion<int>().IsRequired();
        builder.Property(item => item.DailyCommitmentMinutes).IsRequired();
        builder.Property(item => item.WeeklyTargetDays).IsRequired();
        builder.Property(item => item.CompletionType)
            .HasConversion<int>().IsRequired();
        builder.Property(item => item.OnboardingCompletedAtUtc).IsRequired();
        builder.Property(item => item.CreatedAtUtc).IsRequired();
        builder.Property(item => item.UpdatedAtUtc).IsRequired();
        builder.Property(item => item.Version)
            .IsRequired().IsConcurrencyToken();
        builder.HasOne<User>().WithOne()
            .HasForeignKey<UserLearningPreference>(item => item.UserId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasMany(item => item.FocusAreas).WithOne()
            .HasForeignKey(item => item.UserId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(item => item.FocusAreas)
            .UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
