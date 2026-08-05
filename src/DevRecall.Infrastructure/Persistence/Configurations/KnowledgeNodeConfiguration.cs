using DevRecall.Domain.Identity;
using DevRecall.Domain.Knowledge;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DevRecall.Infrastructure.Persistence.Configurations;

internal sealed class KnowledgeNodeConfiguration
    : IEntityTypeConfiguration<KnowledgeNode>
{
    public void Configure(EntityTypeBuilder<KnowledgeNode> builder)
    {
        builder.ToTable("knowledge_nodes");

        builder.HasKey(node => node.Id);

        builder.Property(node => node.Id)
            .ValueGeneratedNever();

        builder.Property(node => node.UserId)
            .IsRequired();

        builder.Property(node => node.ParentId);

        builder.Property(node => node.Title)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(node => node.Content)
            .HasColumnType("text")
            .IsRequired();

        builder.Property(node => node.Description)
            .HasMaxLength(500);

        builder.Property(node => node.SourceUrl)
            .HasMaxLength(2048);

        builder.Property(node => node.SortOrder)
            .IsRequired();

        builder.Property(node => node.Status)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(node => node.CreatedAtUtc)
            .IsRequired();

        builder.Property(node => node.UpdatedAtUtc)
            .IsRequired();

        builder.Property(node => node.Version)
            .IsConcurrencyToken()
            .IsRequired();

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(node => node.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<KnowledgeNode>()
            .WithMany()
            .HasForeignKey(node => node.ParentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(node => node.UserId)
            .HasDatabaseName("ix_knowledge_nodes_user_id");

        builder.HasIndex(node => new { node.UserId, node.ParentId })
            .HasDatabaseName("ix_knowledge_nodes_user_id_parent_id");

        builder.HasIndex(node => new { node.UserId, node.ParentId, node.SortOrder })
            .HasDatabaseName("ix_knowledge_nodes_user_parent_sort_order");
    }
}
