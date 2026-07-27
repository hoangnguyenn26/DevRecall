using DevRecall.Domain.Interview;
using DevRecall.Domain.Interview.Answers;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DevRecall.Infrastructure.Persistence.Configurations;

internal sealed class InterviewAnswerVersionConfiguration
    : IEntityTypeConfiguration<InterviewAnswerVersion>
{
    public void Configure(EntityTypeBuilder<InterviewAnswerVersion> builder)
    {
        builder.ToTable("interview_answer_versions");
        builder.HasKey(answer => answer.Id);

        builder.Property(answer => answer.Id).ValueGeneratedNever();
        builder.Property(answer => answer.InterviewQuestionId).IsRequired();
        builder.Property(answer => answer.VersionNumber).IsRequired();
        builder.Property(answer => answer.Content)
            .HasColumnType("text")
            .IsRequired();
        builder.Property(answer => answer.Status)
            .HasConversion<int>()
            .IsRequired();
        builder.Property(answer => answer.CreatedAtUtc).IsRequired();
        builder.Property(answer => answer.UpdatedAtUtc).IsRequired();
        builder.Property(answer => answer.PublishedAtUtc);

        builder.HasOne<InterviewQuestion>()
            .WithMany()
            .HasForeignKey(answer => answer.InterviewQuestionId)
            .OnDelete(DeleteBehavior.Cascade);

        ConfigureIndexes(builder);
    }

    private static void ConfigureIndexes(
        EntityTypeBuilder<InterviewAnswerVersion> builder)
    {
        builder.HasIndex(answer => new
        {
            answer.InterviewQuestionId,
            answer.VersionNumber
        })
            .IsUnique()
            .HasDatabaseName(
                "ux_interview_answer_versions_question_version");

        builder.HasIndex(answer => new
        {
            answer.InterviewQuestionId,
            answer.Status
        })
            .HasDatabaseName(
                "ix_interview_answer_versions_question_status");

        builder.HasIndex(answer => new
        {
            answer.InterviewQuestionId,
            answer.Status,
            answer.VersionNumber
        })
            .HasDatabaseName(
                "ix_interview_answer_versions_question_status_version");

        builder.HasIndex(answer => answer.InterviewQuestionId)
            .IsUnique()
            .HasFilter("\"status\" = 1")
            .HasDatabaseName(
                "ux_interview_answer_versions_one_draft_per_question");
    }
}
