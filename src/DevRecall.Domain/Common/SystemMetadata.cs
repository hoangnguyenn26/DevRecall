namespace DevRecall.Domain.Common;

public sealed class SystemMetadata
{
    private SystemMetadata()
    {
    }

    public SystemMetadata(
        Guid id,
        string key,
        string value,
        DateTimeOffset createdAtUtc)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException(
                "Id cannot be empty.",
                nameof(id));
        }

        if (string.IsNullOrWhiteSpace(key))
        {
            throw new ArgumentException(
                "Key is required.",
                nameof(key));
        }

        Id = id;
        Key = key;
        Value = value;
        CreatedAtUtc = createdAtUtc;
    }

    public Guid Id { get; private set; }

    public string Key { get; private set; } = null!;

    public string Value { get; private set; } = null!;

    public DateTimeOffset CreatedAtUtc { get; private set; }
}
