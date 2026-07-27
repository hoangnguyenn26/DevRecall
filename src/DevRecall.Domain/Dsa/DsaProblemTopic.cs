namespace DevRecall.Domain.Dsa;

public sealed class DsaProblemTopic
{
    public const int MaxLength = 100;
    public const int MaximumTopicsPerProblem = 20;

    private DsaProblemTopic()
    {
    }

    private DsaProblemTopic(string name, string normalizedName)
    {
        Name = name;
        NormalizedName = normalizedName;
    }

    public string Name { get; private set; } = null!;
    public string NormalizedName { get; private set; } = null!;

    public static DsaProblemTopic Create(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Topic is required.", nameof(value));
        }

        var name = string.Join(' ', value.Split(
            (char[]?)null, StringSplitOptions.RemoveEmptyEntries));
        if (name.Length > MaxLength)
        {
            throw new ArgumentException(
                $"Topic cannot exceed {MaxLength} characters.", nameof(value));
        }

        return new DsaProblemTopic(name, name.ToUpperInvariant());
    }
}
