using DevRecall.Domain.Identity;
using DevRecall.Domain.Knowledge;
using DevRecall.Domain.LearningContent;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using LearningContentAggregate = DevRecall.Domain.LearningContent.LearningContent;

namespace DevRecall.Infrastructure.Persistence.Configurations;

internal sealed class KnowledgeSourceConfiguration : IEntityTypeConfiguration<KnowledgeSource>
{
    public void Configure(EntityTypeBuilder<KnowledgeSource> builder)
    {
        builder.ToTable("knowledge_sources", table =>
            table.HasCheckConstraint("ck_knowledge_sources_type", "source_type = 1"));
        builder.HasKey(source => source.KnowledgeNodeId);
        builder.Property(source => source.KnowledgeNodeId).ValueGeneratedNever();
        builder.Property(source => source.SourceType).HasConversion<int>();
        builder.Property(source => source.SourceTitleSnapshot).HasMaxLength(200).IsRequired();
        builder.HasIndex(source => new { source.UserId, source.SubmissionId }).IsUnique()
            .HasDatabaseName("ux_knowledge_sources_user_submission");
        builder.HasOne<KnowledgeNode>().WithOne().HasForeignKey<KnowledgeSource>(source => source.KnowledgeNodeId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<User>().WithMany().HasForeignKey(source => source.UserId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<LearningContentAggregate>().WithMany()
            .HasForeignKey(source => source.LearningContentId).OnDelete(DeleteBehavior.Restrict);
    }
}
