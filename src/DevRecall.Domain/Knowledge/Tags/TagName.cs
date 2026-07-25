namespace DevRecall.Domain.Knowledge.Tags;

public static class TagName
{
    public const int MaxLength = 100;

    public static string NormalizeDisplayName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Tag name is required.", nameof(name));
        }

        var normalizedDisplayName = string.Join(
            ' ',
            name.Split(' ', StringSplitOptions.RemoveEmptyEntries));

        if (normalizedDisplayName.Length > MaxLength)
        {
            throw new ArgumentException(
                $"Tag name cannot exceed {MaxLength} characters.",
                nameof(name));
        }

        return normalizedDisplayName;
    }

    public static string NormalizeIdentity(string name) =>
        NormalizeDisplayName(name).ToUpperInvariant();
}
