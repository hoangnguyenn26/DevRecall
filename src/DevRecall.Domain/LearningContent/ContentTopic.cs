namespace DevRecall.Domain.LearningContent;

public sealed class ContentTopic
{
    private ContentTopic() { }

    private ContentTopic(Guid id, string slug, string name, DateTimeOffset currentUtc)
    {
        Id = id;
        Slug = slug;
        Name = name;
        CreatedAtUtc = currentUtc;
        UpdatedAtUtc = currentUtc;
    }

    public Guid Id { get; private set; }
    public string Slug { get; private set; } = null!;
    public string Name { get; private set; } = null!;
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public DateTimeOffset UpdatedAtUtc { get; private set; }

    public static ContentTopic Create(Guid id, string slug, string name, DateTimeOffset currentUtc)
    {
        if (id == Guid.Empty) throw new ArgumentException("Topic id cannot be empty.", nameof(id));
        EnsureUtc(currentUtc);
        return new ContentTopic(id, LearningContentText.NormalizeSlug(slug),
            LearningContentText.NormalizeRequired(name, nameof(name), 100, 2), currentUtc);
    }

    private static void EnsureUtc(DateTimeOffset value)
    {
        if (value.Offset != TimeSpan.Zero) throw new ArgumentException("Timestamp must be UTC.", nameof(value));
    }
}
