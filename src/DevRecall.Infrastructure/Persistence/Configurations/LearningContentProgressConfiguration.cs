using DevRecall.Domain.Identity;
using DevRecall.Domain.LearningContent;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using LearningContentAggregate = DevRecall.Domain.LearningContent.LearningContent;

namespace DevRecall.Infrastructure.Persistence.Configurations;

internal sealed class LearningContentProgressConfiguration : IEntityTypeConfiguration<LearningContentProgress>
{
    public void Configure(EntityTypeBuilder<LearningContentProgress> builder)
    {
        builder.ToTable("learning_content_progresses", table =>
        {
            table.HasCheckConstraint("ck_learning_content_progresses_status", "status BETWEEN 1 AND 2");
            table.HasCheckConstraint("ck_learning_content_progresses_version", "version > 0");
        });
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.Status).HasConversion<int>();
        builder.Property(x => x.Version).IsConcurrencyToken();
        builder.HasIndex(x => new { x.UserId, x.LearningContentId }).IsUnique()
            .HasDatabaseName("uq_learning_content_progresses_user_content");
        builder.HasOne<User>().WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<LearningContentAggregate>().WithMany().HasForeignKey(x => x.LearningContentId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class LearningContentCompletionEvidenceConfiguration
    : IEntityTypeConfiguration<LearningContentCompletionEvidence>
{
    public void Configure(EntityTypeBuilder<LearningContentCompletionEvidence> builder)
    {
        builder.ToTable("learning_content_completion_evidence");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.TitleSnapshot).HasMaxLength(200).IsRequired();
        builder.HasIndex(x => new { x.UserId, x.LearningContentId }).IsUnique()
            .HasDatabaseName("uq_learning_content_completion_evidence_user_content");
        builder.HasOne<User>().WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<LearningContentAggregate>().WithMany().HasForeignKey(x => x.LearningContentId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
