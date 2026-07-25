namespace DevRecall.Domain.Knowledge.Tags;

public sealed class Tag
{
    private Tag()
    {
    }

    private Tag(Guid id, Guid userId, string name, string normalizedName,
        DateTimeOffset createdAtUtc)
    {
        Id = id;
        UserId = userId;
        Name = name;
        NormalizedName = normalizedName;
        Status = TagStatus.Active;
        CreatedAtUtc = createdAtUtc;
        UpdatedAtUtc = createdAtUtc;
    }

    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public string Name { get; private set; } = null!;
    public string NormalizedName { get; private set; } = null!;
    public TagStatus Status { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public DateTimeOffset UpdatedAtUtc { get; private set; }

    public static Tag Create(Guid id, Guid userId, string name,
        DateTimeOffset createdAtUtc)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("Tag id cannot be empty.", nameof(id));
        }

        if (userId == Guid.Empty)
        {
            throw new ArgumentException("User id cannot be empty.", nameof(userId));
        }

        var displayName = TagName.NormalizeDisplayName(name);
        var normalizedName = TagName.NormalizeIdentity(name);

        return new Tag(id, userId, displayName, normalizedName, createdAtUtc);
    }

    public bool Rename(string name, DateTimeOffset updatedAtUtc)
    {
        var displayName = TagName.NormalizeDisplayName(name);
        var normalizedName = TagName.NormalizeIdentity(name);

        if (string.Equals(Name, displayName, StringComparison.Ordinal)
            && string.Equals(NormalizedName, normalizedName, StringComparison.Ordinal))
        {
            return false;
        }

        Name = displayName;
        NormalizedName = normalizedName;
        UpdatedAtUtc = updatedAtUtc;

        return true;
    }

    public bool Archive(DateTimeOffset updatedAtUtc)
    {
        if (Status == TagStatus.Archived)
        {
            return false;
        }

        Status = TagStatus.Archived;
        UpdatedAtUtc = updatedAtUtc;

        return true;
    }
}
