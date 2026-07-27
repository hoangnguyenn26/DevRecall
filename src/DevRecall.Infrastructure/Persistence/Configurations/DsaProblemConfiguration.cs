using DevRecall.Domain.Dsa;
using DevRecall.Domain.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DevRecall.Infrastructure.Persistence.Configurations;

internal sealed class DsaProblemConfiguration
    : IEntityTypeConfiguration<DsaProblem>
{
    public void Configure(EntityTypeBuilder<DsaProblem> builder)
    {
        builder.ToTable("dsa_problems");
        builder.HasKey(problem => problem.Id);
        builder.Property(problem => problem.Id).ValueGeneratedNever();
        builder.Property(problem => problem.UserId).IsRequired();
        builder.Property(problem => problem.Title)
            .HasMaxLength(DsaProblemText.TitleMaxLength)
            .IsRequired();
        builder.Property(problem => problem.Description)
            .HasColumnType("text")
            .IsRequired();
        builder.Property(problem => problem.Difficulty)
            .HasConversion<int>()
            .IsRequired();
        builder.Property(problem => problem.Source)
            .HasMaxLength(DsaProblemText.SourceMaxLength);
        builder.Property(problem => problem.ExternalUrl)
            .HasMaxLength(DsaProblemText.ExternalUrlMaxLength);
        builder.Property(problem => problem.Status)
            .HasConversion<int>()
            .IsRequired();
        builder.Property(problem => problem.CreatedAtUtc).IsRequired();
        builder.Property(problem => problem.UpdatedAtUtc).IsRequired();

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(problem => problem.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        ConfigureTopics(builder);
        ConfigureIndexes(builder);
    }

    private static void ConfigureTopics(EntityTypeBuilder<DsaProblem> builder)
    {
        builder.OwnsMany(problem => problem.Topics, topicBuilder =>
        {
            topicBuilder.ToTable("dsa_problem_topics");
            topicBuilder.WithOwner().HasForeignKey("dsa_problem_id");
            topicBuilder.Property(topic => topic.Name)
                .HasMaxLength(DsaProblemTopic.MaxLength)
                .IsRequired();
            topicBuilder.Property(topic => topic.NormalizedName)
                .HasMaxLength(DsaProblemTopic.MaxLength)
                .IsRequired();
            topicBuilder.HasKey(
                "dsa_problem_id",
                nameof(DsaProblemTopic.NormalizedName));
            topicBuilder.HasIndex(topic => topic.NormalizedName)
                .HasDatabaseName(
                    "ix_dsa_problem_topics_normalized_name");
        });

        builder.Navigation(problem => problem.Topics)
            .HasField("_topics")
            .UsePropertyAccessMode(PropertyAccessMode.Field);
    }

    private static void ConfigureIndexes(EntityTypeBuilder<DsaProblem> builder)
    {
        builder.HasIndex(problem => new
        {
            problem.UserId,
            problem.Status,
            problem.UpdatedAtUtc
        })
            .HasDatabaseName(
                "ix_dsa_problems_user_status_updated_at");

        builder.HasIndex(problem => new
        {
            problem.UserId,
            problem.Difficulty,
            problem.Status
        })
            .HasDatabaseName(
                "ix_dsa_problems_user_difficulty_status");

        builder.HasIndex(problem => new
        {
            problem.UserId,
            problem.Source,
            problem.Status
        })
            .HasDatabaseName(
                "ix_dsa_problems_user_source_status");
    }
}
