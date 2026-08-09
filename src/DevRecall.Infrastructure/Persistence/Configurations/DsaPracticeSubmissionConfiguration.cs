using DevRecall.Domain.Dsa;
using DevRecall.Domain.Dsa.Attempts;
using DevRecall.Domain.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DevRecall.Infrastructure.Persistence.Configurations;

internal sealed class DsaPracticeSubmissionConfiguration : IEntityTypeConfiguration<DsaPracticeSubmission>
{
    public void Configure(EntityTypeBuilder<DsaPracticeSubmission> builder)
    {
        builder.ToTable("dsa_practice_submissions");
        builder.HasKey(item => item.Id);
        builder.Property(item => item.Id).ValueGeneratedNever();
        builder.Property(item => item.ProblemTitleSnapshot).HasMaxLength(DsaProblemText.TitleMaxLength).IsRequired();
        builder.Property(item => item.DifficultySnapshot).HasMaxLength(20).IsRequired();
        builder.HasIndex(item => new { item.UserId, item.SubmissionId }).IsUnique()
            .HasDatabaseName("ux_dsa_practice_submissions_user_submission");
        builder.HasOne<User>().WithMany().HasForeignKey(item => item.UserId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<DsaProblem>().WithMany().HasForeignKey(item => item.DsaProblemId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<DsaAttempt>().WithOne().HasForeignKey<DsaPracticeSubmission>(item => item.DsaAttemptId).OnDelete(DeleteBehavior.Cascade);
    }
}
