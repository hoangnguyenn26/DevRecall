using DevRecall.Domain.Identity;
using DevRecall.Domain.LearningProfiles;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DevRecall.Infrastructure.Persistence.Configurations;

internal sealed class LearningProfileConfiguration : IEntityTypeConfiguration<LearningProfile>
{
    public void Configure(EntityTypeBuilder<LearningProfile> builder)
    {
        builder.ToTable("learning_profiles", table =>
        {
            table.HasCheckConstraint("ck_learning_profiles_role", "target_role BETWEEN 1 AND 7");
            table.HasCheckConstraint("ck_learning_profiles_level", "experience_level BETWEEN 1 AND 4");
            table.HasCheckConstraint("ck_learning_profiles_minutes", "available_minutes_per_day BETWEEN 5 AND 480");
            table.HasCheckConstraint("ck_learning_profiles_version", "version > 0");
        });
        builder.HasKey(item => item.Id);
        builder.Property(item => item.Id).ValueGeneratedNever();
        builder.HasIndex(item => item.UserId).IsUnique()
            .HasDatabaseName("uq_learning_profiles_user_id");
        builder.Property(item => item.TargetRole).HasConversion<int>().IsRequired();
        builder.Property(item => item.ExperienceLevel).HasConversion<int>().IsRequired();
        builder.Property(item => item.Version).IsConcurrencyToken();
        builder.HasOne<User>().WithOne().HasForeignKey<LearningProfile>(item => item.UserId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasMany(item => item.Technologies).WithOne()
            .HasForeignKey(item => item.LearningProfileId).OnDelete(DeleteBehavior.Cascade);
        builder.HasMany(item => item.Goals).WithOne()
            .HasForeignKey(item => item.LearningProfileId).OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(item => item.Technologies).UsePropertyAccessMode(PropertyAccessMode.Field);
        builder.Navigation(item => item.Goals).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}

internal sealed class LearningProfileTechnologyConfiguration : IEntityTypeConfiguration<LearningProfileTechnology>
{
    public void Configure(EntityTypeBuilder<LearningProfileTechnology> builder)
    {
        builder.ToTable("learning_profile_technologies", table =>
            table.HasCheckConstraint("ck_learning_profile_technologies_value", "technology BETWEEN 1 AND 17"));
        builder.HasKey(item => item.Id);
        builder.Property(item => item.Id).ValueGeneratedNever();
        builder.Property(item => item.Technology).HasConversion<int>();
        builder.HasIndex(item => new { item.LearningProfileId, item.Technology }).IsUnique()
            .HasDatabaseName("uq_learning_profile_technologies_profile_technology");
    }
}

internal sealed class LearningProfileGoalEntryConfiguration : IEntityTypeConfiguration<LearningProfileGoalEntry>
{
    public void Configure(EntityTypeBuilder<LearningProfileGoalEntry> builder)
    {
        builder.ToTable("learning_profile_goals", table =>
            table.HasCheckConstraint("ck_learning_profile_goals_value", "goal BETWEEN 1 AND 6"));
        builder.HasKey(item => item.Id);
        builder.Property(item => item.Id).ValueGeneratedNever();
        builder.Property(item => item.Goal).HasConversion<int>();
        builder.HasIndex(item => new { item.LearningProfileId, item.Goal }).IsUnique()
            .HasDatabaseName("uq_learning_profile_goals_profile_goal");
    }
}
