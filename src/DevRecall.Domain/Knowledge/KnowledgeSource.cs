namespace DevRecall.Domain.Knowledge;

public enum KnowledgeSourceType
{
    LearningContent = 1
}

public sealed class KnowledgeSource
{
    private KnowledgeSource() { }

    private KnowledgeSource(Guid knowledgeNodeId, Guid userId, KnowledgeSourceType sourceType,
        Guid learningContentId, Guid submissionId, string sourceTitleSnapshot,
        DateTimeOffset createdAtUtc)
    {
        KnowledgeNodeId = knowledgeNodeId;
        UserId = userId;
        SourceType = sourceType;
        LearningContentId = learningContentId;
        SubmissionId = submissionId;
        SourceTitleSnapshot = sourceTitleSnapshot;
        CreatedAtUtc = createdAtUtc;
    }

    public Guid KnowledgeNodeId { get; private set; }
    public Guid UserId { get; private set; }
    public KnowledgeSourceType SourceType { get; private set; }
    public Guid LearningContentId { get; private set; }
    public Guid SubmissionId { get; private set; }
    public string SourceTitleSnapshot { get; private set; } = null!;
    public DateTimeOffset CreatedAtUtc { get; private set; }

    public static KnowledgeSource FromLearningContent(Guid knowledgeNodeId, Guid userId,
        Guid learningContentId, Guid submissionId, string sourceTitleSnapshot,
        DateTimeOffset createdAtUtc)
    {
        if (knowledgeNodeId == Guid.Empty || userId == Guid.Empty || learningContentId == Guid.Empty
            || submissionId == Guid.Empty)
            throw new ArgumentException("Knowledge source identifiers cannot be empty.");
        if (string.IsNullOrWhiteSpace(sourceTitleSnapshot))
            throw new ArgumentException("Source title is required.", nameof(sourceTitleSnapshot));
        var title = sourceTitleSnapshot.Trim();
        if (title.Length > 200)
            throw new ArgumentException("Source title cannot exceed 200 characters.", nameof(sourceTitleSnapshot));
        if (createdAtUtc.Offset != TimeSpan.Zero)
            throw new ArgumentException("Timestamp must be UTC.", nameof(createdAtUtc));
        return new KnowledgeSource(knowledgeNodeId, userId, KnowledgeSourceType.LearningContent,
            learningContentId, submissionId, title, createdAtUtc);
    }
}
