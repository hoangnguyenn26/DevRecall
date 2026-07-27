using DevRecall.Domain.Interview;
using DevRecall.Domain.Interview.FollowUps;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DevRecall.Infrastructure.Persistence.Configurations;

internal sealed class InterviewFollowUpQuestionConfiguration
    : IEntityTypeConfiguration<InterviewFollowUpQuestion>
{
    public void Configure(EntityTypeBuilder<InterviewFollowUpQuestion> builder)
    {
        builder.ToTable("interview_follow_up_questions");
        builder.HasKey(followUp => followUp.Id);
        builder.Property(followUp => followUp.Id).ValueGeneratedNever();
        builder.Property(followUp => followUp.InterviewQuestionId).IsRequired();
        builder.Property(followUp => followUp.Prompt)
            .HasMaxLength(InterviewFollowUpPrompt.MaxLength)
            .IsRequired();
        builder.Property(followUp => followUp.SortOrder).IsRequired();
        builder.Property(followUp => followUp.Status)
            .HasConversion<int>()
            .IsRequired();
        builder.Property(followUp => followUp.CreatedAtUtc).IsRequired();
        builder.Property(followUp => followUp.UpdatedAtUtc).IsRequired();

        builder.HasOne<InterviewQuestion>()
            .WithMany()
            .HasForeignKey(followUp => followUp.InterviewQuestionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(followUp => new
        {
            followUp.InterviewQuestionId,
            followUp.Status,
            followUp.SortOrder
        })
            .HasDatabaseName(
                "ix_interview_follow_ups_question_status_order");
    }
}
