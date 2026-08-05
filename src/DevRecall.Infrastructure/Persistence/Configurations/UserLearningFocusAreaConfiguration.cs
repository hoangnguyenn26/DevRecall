using DevRecall.Domain.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DevRecall.Infrastructure.Persistence.Configurations;

internal sealed class UserLearningFocusAreaConfiguration
    : IEntityTypeConfiguration<UserLearningFocusArea>
{
    public void Configure(EntityTypeBuilder<UserLearningFocusArea> builder)
    {
        builder.ToTable("user_learning_focus_areas", table =>
            table.HasCheckConstraint("ck_user_learning_focus_areas_area",
                "area BETWEEN 1 AND 6"));
        builder.HasKey(item => new { item.UserId, item.Area });
        builder.Property(item => item.UserId).ValueGeneratedNever();
        builder.Property(item => item.Area).HasConversion<int>();
    }
}
