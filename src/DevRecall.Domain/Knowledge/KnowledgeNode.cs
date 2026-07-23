namespace DevRecall.Domain.Knowledge;

public sealed class KnowledgeNode
{
    private const int MaximumTitleLength = 200;

    private KnowledgeNode()
    {
    }

    private KnowledgeNode(Guid id, Guid userId, Guid? parentId,
        string title, DateTimeOffset createdAtUtc)
    {
        Id = id;
        UserId = userId;
        ParentId = parentId;
        Title = title;
        Status = KnowledgeNodeStatus.Active;
        CreatedAtUtc = createdAtUtc;
        UpdatedAtUtc = createdAtUtc;
    }

    public Guid Id { get; private set; }

    public Guid UserId { get; private set; }

    public Guid? ParentId { get; private set; }

    public string Title { get; private set; } = null!;

    public KnowledgeNodeStatus Status { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; private set; }

    public DateTimeOffset UpdatedAtUtc { get; private set; }

    public static KnowledgeNode Create(Guid id, Guid userId, Guid? parentId,
        string title, DateTimeOffset createdAtUtc)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException(
                "Knowledge node id cannot be empty.",
                nameof(id));
        }

        if (userId == Guid.Empty)
        {
            throw new ArgumentException(
                "User id cannot be empty.",
                nameof(userId));
        }

        if (parentId == id)
        {
            throw new InvalidOperationException(
                "A knowledge node cannot be its own parent.");
        }

        return new KnowledgeNode(
            id,
            userId,
            parentId,
            NormalizeTitle(title),
            createdAtUtc);
    }

    public void Rename(string title, DateTimeOffset updatedAtUtc)
    {
        Title = NormalizeTitle(title);
        UpdatedAtUtc = updatedAtUtc;
    }

    public void Archive(DateTimeOffset updatedAtUtc)
    {
        Status = KnowledgeNodeStatus.Archived;
        UpdatedAtUtc = updatedAtUtc;
    }

    private static string NormalizeTitle(string title)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            throw new ArgumentException(
                "Title is required.",
                nameof(title));
        }

        var normalizedTitle = title.Trim();

        if (normalizedTitle.Length > MaximumTitleLength)
        {
            throw new ArgumentException(
                "Title cannot exceed 200 characters.",
                nameof(title));
        }

        return normalizedTitle;
    }
}
