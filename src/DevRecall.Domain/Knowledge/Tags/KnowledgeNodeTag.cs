namespace DevRecall.Domain.Knowledge.Tags;

public sealed class KnowledgeNodeTag
{
    private KnowledgeNodeTag()
    {
    }

    private KnowledgeNodeTag(Guid knowledgeNodeId, Guid tagId,
        DateTimeOffset createdAtUtc)
    {
        KnowledgeNodeId = knowledgeNodeId;
        TagId = tagId;
        CreatedAtUtc = createdAtUtc;
    }

    public Guid KnowledgeNodeId { get; private set; }
    public Guid TagId { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }

    public static KnowledgeNodeTag Create(Guid knowledgeNodeId, Guid tagId,
        DateTimeOffset createdAtUtc)
    {
        if (knowledgeNodeId == Guid.Empty)
        {
            throw new ArgumentException(
                "Knowledge node id cannot be empty.",
                nameof(knowledgeNodeId));
        }

        if (tagId == Guid.Empty)
        {
            throw new ArgumentException("Tag id cannot be empty.", nameof(tagId));
        }

        return new KnowledgeNodeTag(knowledgeNodeId, tagId, createdAtUtc);
    }
}
