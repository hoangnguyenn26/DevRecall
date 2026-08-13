using DevRecall.Domain.Identity;
using DevRecall.Domain.LearningContent;
using DevRecall.Domain.Reviews;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using LearningContentAggregate = DevRecall.Domain.LearningContent.LearningContent;

namespace DevRecall.Infrastructure.Persistence.Configurations;

internal sealed class ReviewLearningContentSourceConfiguration
    : IEntityTypeConfiguration<ReviewLearningContentSource>
{
    public void Configure(EntityTypeBuilder<ReviewLearningContentSource> builder)
    {
        builder.ToTable("review_learning_content_sources");
        builder.HasKey(source => source.ReviewItemId);
        builder.Property(source => source.ReviewItemId).ValueGeneratedNever();
        builder.Property(source => source.CandidateKey).HasMaxLength(100).IsRequired();
        builder.Property(source => source.SourceTitleSnapshot).HasMaxLength(200).IsRequired();
        builder.Property(source => source.PromptSnapshot).HasMaxLength(500).IsRequired();
        builder.Property(source => source.AnswerSnapshot).HasMaxLength(2_000).IsRequired();
        builder.HasOne<ReviewItem>().WithOne()
            .HasForeignKey<ReviewLearningContentSource>(source => source.ReviewItemId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<User>().WithMany().HasForeignKey(source => source.UserId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<LearningContentAggregate>().WithMany()
            .HasForeignKey(source => source.LearningContentId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<LearningReviewCandidate>().WithMany()
            .HasForeignKey(source => source.CandidateId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(source => new { source.UserId, source.LearningContentId, source.CandidateKey })
            .HasDatabaseName("ix_review_learning_sources_user_content_candidate");
    }
}

internal sealed class LearningContentReviewSubmissionConfiguration
    : IEntityTypeConfiguration<LearningContentReviewSubmission>
{
    public void Configure(EntityTypeBuilder<LearningContentReviewSubmission> builder)
    {
        builder.ToTable("learning_content_review_submissions", table =>
        {
            table.HasCheckConstraint("ck_learning_review_submissions_created_count", "created_count >= 0");
            table.HasCheckConstraint("ck_learning_review_submissions_existing_count", "existing_count >= 0");
        });
        builder.HasKey(item => new { item.UserId, item.SubmissionId });
        builder.HasOne<User>().WithMany().HasForeignKey(item => item.UserId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<LearningContentAggregate>().WithMany()
            .HasForeignKey(item => item.LearningContentId).OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class LearningContentReviewSubmissionItemConfiguration
    : IEntityTypeConfiguration<LearningContentReviewSubmissionItem>
{
    public void Configure(EntityTypeBuilder<LearningContentReviewSubmissionItem> builder)
    {
        builder.ToTable("learning_content_review_submission_items");
        builder.HasKey(item => new { item.UserId, item.SubmissionId, item.CandidateKey });
        builder.Property(item => item.CandidateKey).HasMaxLength(100);
        builder.HasOne<LearningContentReviewSubmission>().WithMany()
            .HasForeignKey(item => new { item.UserId, item.SubmissionId }).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<ReviewItem>().WithMany().HasForeignKey(item => item.ReviewItemId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
