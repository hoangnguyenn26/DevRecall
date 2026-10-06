using DevRecall.Domain.LearningContent;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using LearningContentAggregate = DevRecall.Domain.LearningContent.LearningContent;

namespace DevRecall.Infrastructure.Persistence.Configurations;

internal sealed class LearningContentConfiguration : IEntityTypeConfiguration<LearningContentAggregate>
{
    public void Configure(EntityTypeBuilder<LearningContentAggregate> builder)
    {
        builder.ToTable("learning_contents", table =>
        {
            table.HasCheckConstraint("ck_learning_contents_type", "content_type BETWEEN 1 AND 2");
            table.HasCheckConstraint("ck_learning_contents_difficulty", "difficulty BETWEEN 1 AND 3");
            table.HasCheckConstraint("ck_learning_contents_status", "status BETWEEN 1 AND 3");
            table.HasCheckConstraint("ck_learning_contents_source_type", "source_type BETWEEN 1 AND 2");
            table.HasCheckConstraint("ck_learning_contents_minutes", "estimated_minutes BETWEEN 1 AND 480");
            table.HasCheckConstraint("ck_learning_contents_version", "version > 0");
        });
        builder.HasKey(item => item.Id);
        builder.Property(item => item.Id).ValueGeneratedNever();
        builder.Property(item => item.Slug).HasMaxLength(160).IsRequired();
        builder.HasIndex(item => item.Slug).IsUnique().HasDatabaseName("uq_learning_contents_slug");
        builder.Property(item => item.Title).HasMaxLength(200).IsRequired();
        builder.Property(item => item.Summary).HasMaxLength(500).IsRequired();
        builder.Property(item => item.ContentType).HasConversion<int>();
        builder.Property(item => item.Difficulty).HasConversion<int>();
        builder.Property(item => item.Status).HasConversion<int>();
        builder.Property(item => item.SourceType).HasConversion<int>();
        builder.Property(item => item.SourceName).HasMaxLength(150).IsRequired();
        builder.Property(item => item.SourceUrl).HasMaxLength(2_048);
        builder.Property(item => item.Version).IsConcurrencyToken();
        builder.HasIndex(item => new { item.Status, item.PublishedAtUtc })
            .HasDatabaseName("ix_learning_contents_status_published_at_utc");
        builder.HasMany(item => item.Technologies).WithOne()
            .HasForeignKey(item => item.LearningContentId).OnDelete(DeleteBehavior.Cascade);
        builder.HasMany(item => item.Goals).WithOne()
            .HasForeignKey(item => item.LearningContentId).OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(item => item.Goals).UsePropertyAccessMode(PropertyAccessMode.Field);
        builder.HasMany(item => item.Topics).WithOne()
            .HasForeignKey(item => item.LearningContentId).OnDelete(DeleteBehavior.Cascade);
        builder.HasMany(item => item.Objectives).WithOne()
            .HasForeignKey(item => item.LearningContentId).OnDelete(DeleteBehavior.Cascade);
        builder.HasMany(item => item.Sections).WithOne()
            .HasForeignKey(item => item.LearningContentId).OnDelete(DeleteBehavior.Cascade);
        builder.HasMany(item => item.ReviewCandidates).WithOne()
            .HasForeignKey(item => item.LearningContentId).OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(item => item.Technologies).UsePropertyAccessMode(PropertyAccessMode.Field);
        builder.Navigation(item => item.Topics).UsePropertyAccessMode(PropertyAccessMode.Field);
        builder.Navigation(item => item.Objectives).UsePropertyAccessMode(PropertyAccessMode.Field);
        builder.Navigation(item => item.Sections).UsePropertyAccessMode(PropertyAccessMode.Field);
        builder.Navigation(item => item.ReviewCandidates).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}

internal sealed class LearningContentGoalConfiguration : IEntityTypeConfiguration<LearningContentGoal>
{
    public void Configure(EntityTypeBuilder<LearningContentGoal> builder)
    {
        builder.ToTable("learning_content_goals", table =>
            table.HasCheckConstraint("ck_learning_content_goals_value", "goal BETWEEN 1 AND 6"));
        builder.HasKey(item => new { item.LearningContentId, item.Goal });
        builder.Property(item => item.Goal).HasConversion<int>();
    }
}

internal sealed class LearningReviewCandidateConfiguration
    : IEntityTypeConfiguration<LearningReviewCandidate>
{
    public void Configure(EntityTypeBuilder<LearningReviewCandidate> builder)
    {
        builder.ToTable("learning_content_review_candidates", table =>
            table.HasCheckConstraint("ck_learning_review_candidates_position", "position >= 0"));
        builder.HasKey(item => item.Id);
        builder.Property(item => item.Id).ValueGeneratedNever();
        builder.Property(item => item.Key).HasMaxLength(100).IsRequired();
        builder.Property(item => item.Prompt).HasMaxLength(500).IsRequired();
        builder.Property(item => item.Answer).HasMaxLength(2_000).IsRequired();
        builder.HasIndex(item => new { item.LearningContentId, item.Key }).IsUnique()
            .HasDatabaseName("ux_learning_review_candidates_content_key");
        builder.HasIndex(item => new { item.LearningContentId, item.Position }).IsUnique()
            .HasDatabaseName("ux_learning_review_candidates_content_position");
    }
}

internal sealed class ContentTopicConfiguration : IEntityTypeConfiguration<ContentTopic>
{
    public void Configure(EntityTypeBuilder<ContentTopic> builder)
    {
        builder.ToTable("content_topics");
        builder.HasKey(item => item.Id);
        builder.Property(item => item.Id).ValueGeneratedNever();
        builder.Property(item => item.Slug).HasMaxLength(160).IsRequired();
        builder.Property(item => item.Name).HasMaxLength(100).IsRequired();
        builder.HasIndex(item => item.Slug).IsUnique().HasDatabaseName("uq_content_topics_slug");
    }
}

internal sealed class LearningContentTechnologyConfiguration
    : IEntityTypeConfiguration<LearningContentTechnology>
{
    public void Configure(EntityTypeBuilder<LearningContentTechnology> builder)
    {
        builder.ToTable("learning_content_technologies", table =>
            table.HasCheckConstraint("ck_learning_content_technologies_value", "technology BETWEEN 1 AND 17"));
        builder.HasKey(item => item.Id);
        builder.Property(item => item.Id).ValueGeneratedNever();
        builder.Property(item => item.Technology).HasConversion<int>();
        builder.HasIndex(item => new { item.LearningContentId, item.Technology }).IsUnique()
            .HasDatabaseName("uq_learning_content_technologies_content_technology");
        builder.HasIndex(item => new { item.Technology, item.LearningContentId })
            .HasDatabaseName("ix_learning_content_technologies_technology_content");
    }
}

internal sealed class LearningContentTopicConfiguration : IEntityTypeConfiguration<LearningContentTopic>
{
    public void Configure(EntityTypeBuilder<LearningContentTopic> builder)
    {
        builder.ToTable("learning_content_topics");
        builder.HasKey(item => new { item.LearningContentId, item.TopicId });
        builder.HasOne<ContentTopic>().WithMany().HasForeignKey(item => item.TopicId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(item => new { item.TopicId, item.LearningContentId })
            .HasDatabaseName("ix_learning_content_topics_topic_content");
    }
}

internal sealed class LearningObjectiveConfiguration : IEntityTypeConfiguration<LearningObjective>
{
    public void Configure(EntityTypeBuilder<LearningObjective> builder)
    {
        builder.ToTable("learning_objectives", table =>
            table.HasCheckConstraint("ck_learning_objectives_position", "position >= 0"));
        builder.HasKey(item => item.Id);
        builder.Property(item => item.Id).ValueGeneratedNever();
        builder.Property(item => item.Text).HasMaxLength(300).IsRequired();
        builder.HasIndex(item => item.LearningContentId);
    }
}

internal sealed class LearningContentSectionConfiguration
    : IEntityTypeConfiguration<LearningContentSection>
{
    public void Configure(EntityTypeBuilder<LearningContentSection> builder)
    {
        builder.ToTable("learning_content_sections", table =>
        {
            table.HasCheckConstraint("ck_learning_content_sections_position", "position >= 0");
            table.HasCheckConstraint("ck_learning_content_sections_type", "section_type BETWEEN 1 AND 3");
        });
        builder.HasKey(item => item.Id);
        builder.Property(item => item.Id).ValueGeneratedNever();
        builder.Property(item => item.SectionType).HasConversion<int>();
        builder.Property(item => item.Heading).HasMaxLength(200);
        builder.Property(item => item.BodyMarkdown).HasMaxLength(LearningContentAggregate.MaximumSectionBodyLength).IsRequired();
        builder.HasIndex(item => item.LearningContentId);
    }
}
