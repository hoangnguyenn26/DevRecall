namespace DevRecall.Domain.Knowledge;

public sealed class KnowledgeNode
{
    private const int MaximumContentLength = 100_000;
    private const int MaximumDescriptionLength = 500;
    private const int MaximumSourceUrlLength = 2_048;
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

    public string? Description { get; private set; }

    public string? SourceUrl { get; private set; }

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

    public bool UpdateContent(string? content, DateTimeOffset updatedAtUtc)
    {
        var normalizedContent = NormalizeContent(content);

        if (string.Equals(Content, normalizedContent, StringComparison.Ordinal))
        {
            return false;
        }

        Content = normalizedContent;
        UpdatedAtUtc = updatedAtUtc;
        return true;
    }

    public void UpdateMetadata(
        string? description,
        string? sourceUrl,
        DateTimeOffset updatedAtUtc)
    {
        var normalizedDescription = string.IsNullOrWhiteSpace(description)
            ? null
            : description.Trim();
        var normalizedSourceUrl = string.IsNullOrWhiteSpace(sourceUrl)
            ? null
            : sourceUrl.Trim();

        if (normalizedDescription is not null && normalizedDescription.Length > MaximumDescriptionLength)
        {
            throw new ArgumentException(
                $"Description cannot exceed {MaximumDescriptionLength} characters.",
                nameof(description));
        }

        if (normalizedSourceUrl is not null
            && (normalizedSourceUrl.Length > MaximumSourceUrlLength
                || !Uri.TryCreate(normalizedSourceUrl, UriKind.Absolute, out _)))
        {
            throw new ArgumentException(
                "Source URL must be a valid absolute URL and cannot exceed 2048 characters.",
                nameof(sourceUrl));
        }

        Description = normalizedDescription;
        SourceUrl = normalizedSourceUrl;
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

    private static string NormalizeContent(string? content)
    {
        var normalizedContent = content?.Trim() ?? string.Empty;

        if (normalizedContent.Length > MaximumContentLength)
        {
            throw new ArgumentException(
                $"Content cannot exceed {MaximumContentLength} characters.",
                nameof(content));
        }

        return normalizedContent;
    }
}
