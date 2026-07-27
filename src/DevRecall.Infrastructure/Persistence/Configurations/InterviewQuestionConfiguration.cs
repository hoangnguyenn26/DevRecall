using DevRecall.Domain.Identity;
using DevRecall.Domain.Interview;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DevRecall.Infrastructure.Persistence.Configurations;

internal sealed class InterviewQuestionConfiguration
    : IEntityTypeConfiguration<InterviewQuestion>
{
    public void Configure(EntityTypeBuilder<InterviewQuestion> builder)
    {
        builder.ToTable("interview_questions");
        builder.HasKey(question => question.Id);

        builder.Property(question => question.Id).ValueGeneratedNever();
        builder.Property(question => question.UserId).IsRequired();
        builder.Property(question => question.Title)
            .HasMaxLength(InterviewQuestionText.TitleMaxLength)
            .IsRequired();
        builder.Property(question => question.Question)
            .HasColumnType("text")
            .IsRequired();
        builder.Property(question => question.Topic)
            .HasMaxLength(InterviewQuestionText.TopicMaxLength)
            .IsRequired();
        builder.Property(question => question.Difficulty)
            .HasConversion<int>()
            .IsRequired();
        builder.Property(question => question.Notes).HasColumnType("text");
        builder.Property(question => question.Status)
            .HasConversion<int>()
            .IsRequired();
        builder.Property(question => question.CreatedAtUtc).IsRequired();
        builder.Property(question => question.UpdatedAtUtc).IsRequired();

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(question => question.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        ConfigureIndexes(builder);
    }

    private static void ConfigureIndexes(
        EntityTypeBuilder<InterviewQuestion> builder)
    {
        builder.HasIndex(question => new
        {
            question.UserId,
            question.Status,
            question.UpdatedAtUtc
        })
            .HasDatabaseName(
                "ix_interview_questions_user_status_updated_at");

        builder.HasIndex(question => new
        {
            question.UserId,
            question.Topic,
            question.Status
        })
            .HasDatabaseName(
                "ix_interview_questions_user_topic_status");

        builder.HasIndex(question => new
        {
            question.UserId,
            question.Difficulty,
            question.Status
        })
            .HasDatabaseName(
                "ix_interview_questions_user_difficulty_status");
    }
}
