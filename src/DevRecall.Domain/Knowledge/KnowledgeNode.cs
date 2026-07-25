namespace DevRecall.Domain.Knowledge;

public sealed class KnowledgeNode
{
    private const int MaximumContentLength = 100_000;
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
        Content = string.Empty;
        Status = KnowledgeNodeStatus.Active;
        CreatedAtUtc = createdAtUtc;
        UpdatedAtUtc = createdAtUtc;
    }

    public Guid Id { get; private set; }

    public Guid UserId { get; private set; }

    public Guid? ParentId { get; private set; }

    public string Title { get; private set; } = null!;

    public string Content { get; private set; } = string.Empty;

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

    public void UpdateContent(string? content, DateTimeOffset updatedAtUtc)
    {
        var normalizedContent = content?.Trim() ?? string.Empty;

        if (normalizedContent.Length > MaximumContentLength)
        {
            throw new ArgumentException(
                $"Content cannot exceed {MaximumContentLength} characters.",
                nameof(content));
        }

        Content = normalizedContent;
        UpdatedAtUtc = updatedAtUtc;
    }

    public void Archive(DateTimeOffset updatedAtUtc)
    {
        Status = KnowledgeNodeStatus.Archived;
        UpdatedAtUtc = updatedAtUtc;
    }

    public void MoveTo(Guid? parentId, DateTimeOffset updatedAtUtc)
    {
        if (parentId == Id)
        {
            throw new InvalidOperationException(
                "A knowledge node cannot be its own parent.");
        }

        ParentId = parentId;
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
