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
        string title, int sortOrder, DateTimeOffset createdAtUtc)
    {
        Id = id;
        UserId = userId;
        ParentId = parentId;
        Title = title;
        SortOrder = sortOrder;
        Content = string.Empty;
        Status = KnowledgeNodeStatus.Active;
        Version = 1;
        CreatedAtUtc = createdAtUtc;
        UpdatedAtUtc = createdAtUtc;
    }

    public Guid Id { get; private set; }

    public Guid UserId { get; private set; }

    public Guid? ParentId { get; private set; }

    public string Title { get; private set; } = null!;

    public int SortOrder { get; private set; }

    public string Content { get; private set; } = string.Empty;

    public string? Description { get; private set; }

    public string? SourceUrl { get; private set; }

    public KnowledgeNodeStatus Status { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; private set; }

    public DateTimeOffset UpdatedAtUtc { get; private set; }

    public int Version { get; private set; }

    public static KnowledgeNode Create(Guid id, Guid userId, Guid? parentId,
        string title, int sortOrder, DateTimeOffset createdAtUtc)
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

        if (sortOrder < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(sortOrder),
                "Sort order cannot be negative.");
        }

        return new KnowledgeNode(
            id,
            userId,
            parentId,
            NormalizeTitle(title),
            sortOrder,
            createdAtUtc);
    }

    public void Rename(string title, DateTimeOffset updatedAtUtc)
    {
        var normalizedTitle = NormalizeTitle(title);
        if (string.Equals(Title, normalizedTitle, StringComparison.Ordinal)) return;
        Title = normalizedTitle;
        UpdatedAtUtc = updatedAtUtc;
        Version++;
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
        Version++;
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

        if (string.Equals(Description, normalizedDescription, StringComparison.Ordinal)
            && string.Equals(SourceUrl, normalizedSourceUrl, StringComparison.Ordinal)) return;
        Description = normalizedDescription;
        SourceUrl = normalizedSourceUrl;
        UpdatedAtUtc = updatedAtUtc;
        Version++;
    }

    public void Archive(DateTimeOffset updatedAtUtc)
    {
        if (Status == KnowledgeNodeStatus.Archived) return;
        Status = KnowledgeNodeStatus.Archived;
        UpdatedAtUtc = updatedAtUtc;
        Version++;
    }

    public void MoveTo(Guid? parentId, DateTimeOffset updatedAtUtc)
    {
        if (parentId == Id)
        {
            throw new InvalidOperationException(
                "A knowledge node cannot be its own parent.");
        }

        if (ParentId == parentId) return;
        ParentId = parentId;
        UpdatedAtUtc = updatedAtUtc;
        Version++;
    }

    public bool ChangeSortOrder(int sortOrder, DateTimeOffset updatedAtUtc)
    {
        if (sortOrder < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(sortOrder),
                "Sort order cannot be negative.");
        }

        if (SortOrder == sortOrder)
        {
            return false;
        }

        SortOrder = sortOrder;
        UpdatedAtUtc = updatedAtUtc;
        Version++;
        return true;
    }

    public bool ChangePosition(
        Guid? parentId,
        int sortOrder,
        DateTimeOffset updatedAtUtc)
    {
        if (parentId == Id)
        {
            throw new InvalidOperationException(
                "A knowledge node cannot be its own parent.");
        }

        if (sortOrder < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(sortOrder),
                "Sort order cannot be negative.");
        }

        if (ParentId == parentId && SortOrder == sortOrder)
        {
            return false;
        }

        ParentId = parentId;
        SortOrder = sortOrder;
        UpdatedAtUtc = updatedAtUtc;
        Version++;
        return true;
    }

    public bool Update(string title, string? content, Guid? topicId,
        IReadOnlyCollection<Guid> currentTagIds, IReadOnlyCollection<Guid> tagIds,
        DateTimeOffset currentUtc)
    {
        if (topicId == Id) throw new InvalidOperationException("A knowledge node cannot be its own parent.");
        var normalizedTitle = NormalizeTitle(title);
        var normalizedContent = NormalizeContent(content);
        var tagsChanged = !currentTagIds.Order().SequenceEqual(tagIds.Distinct().Order());
        if (string.Equals(Title, normalizedTitle, StringComparison.Ordinal)
            && string.Equals(Content, normalizedContent, StringComparison.Ordinal)
            && ParentId == topicId && !tagsChanged) return false;

        Title = normalizedTitle;
        Content = normalizedContent;
        ParentId = topicId;
        UpdatedAtUtc = currentUtc;
        Version++;
        return true;
    }

    public void MarkTagsChanged(DateTimeOffset currentUtc)
    {
        UpdatedAtUtc = currentUtc;
        Version++;
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
