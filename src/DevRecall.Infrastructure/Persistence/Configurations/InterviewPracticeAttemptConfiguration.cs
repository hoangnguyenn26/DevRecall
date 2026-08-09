using DevRecall.Domain.Identity;
using DevRecall.Domain.Interview;
using DevRecall.Domain.Interview.Practice;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DevRecall.Infrastructure.Persistence.Configurations;

internal sealed class InterviewPracticeAttemptConfiguration : IEntityTypeConfiguration<InterviewPracticeAttempt>
{
    public void Configure(EntityTypeBuilder<InterviewPracticeAttempt> builder)
    {
        builder.ToTable("interview_practice_attempts");
        builder.HasKey(item => item.Id);
        builder.Property(item => item.Id).ValueGeneratedNever();
        builder.Property(item => item.QuestionSnapshot).HasColumnType("text").IsRequired();
        builder.Property(item => item.AnswerSnapshot).HasColumnType("text").IsRequired();
        builder.Property(item => item.ReferenceAnswerSnapshot).HasColumnType("text");
        builder.Property(item => item.SelfRating).HasConversion<int>().IsRequired();
        builder.HasIndex(item => new { item.UserId, item.SubmissionId }).IsUnique()
            .HasDatabaseName("ux_interview_practice_attempts_user_submission");
        builder.HasIndex(item => new { item.UserId, item.QuestionId, item.CompletedAtUtc })
            .HasDatabaseName("ix_interview_practice_attempts_user_question_completed");
        builder.HasOne<User>().WithMany().HasForeignKey(item => item.UserId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<InterviewQuestion>().WithMany().HasForeignKey(item => item.QuestionId).OnDelete(DeleteBehavior.Restrict);
        builder.HasMany(item => item.FollowUps).WithOne().HasForeignKey(item => item.InterviewPracticeAttemptId).OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(item => item.FollowUps).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}

internal sealed class InterviewPracticeFollowUpAttemptConfiguration : IEntityTypeConfiguration<InterviewPracticeFollowUpAttempt>
{
    public void Configure(EntityTypeBuilder<InterviewPracticeFollowUpAttempt> builder)
    {
        builder.ToTable("interview_practice_follow_up_attempts");
        builder.HasKey(item => item.Id);
        builder.Property(item => item.Id).ValueGeneratedNever();
        builder.Property(item => item.QuestionSnapshot).HasColumnType("text").IsRequired();
        builder.Property(item => item.AnswerSnapshot).HasColumnType("text").IsRequired();
        builder.HasIndex(item => new { item.InterviewPracticeAttemptId, item.FollowUpId }).IsUnique()
            .HasDatabaseName("ux_interview_practice_follow_up_attempts_attempt_follow_up");
    }
}
