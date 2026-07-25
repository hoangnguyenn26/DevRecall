using DevRecall.Domain.Knowledge;
using DevRecall.Domain.Knowledge.Tags;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DevRecall.Infrastructure.Persistence.Configurations;

internal sealed class KnowledgeNodeTagConfiguration
    : IEntityTypeConfiguration<KnowledgeNodeTag>
{
    public void Configure(EntityTypeBuilder<KnowledgeNodeTag> builder)
    {
        builder.ToTable("knowledge_node_tags");
        builder.HasKey(relation => new { relation.KnowledgeNodeId, relation.TagId });
        builder.Property(relation => relation.CreatedAtUtc).IsRequired();

        builder.HasOne<KnowledgeNode>()
            .WithMany()
            .HasForeignKey(relation => relation.KnowledgeNodeId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<Tag>()
            .WithMany()
            .HasForeignKey(relation => relation.TagId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(relation => relation.TagId)
            .HasDatabaseName("ix_knowledge_node_tags_tag_id");
    }
}
