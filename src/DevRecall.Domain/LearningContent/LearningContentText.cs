using System.Text.RegularExpressions;

namespace DevRecall.Domain.LearningContent;

internal static partial class LearningContentText
{
    public static string NormalizeRequired(string value, string parameter, int maximumLength, int minimumLength = 1)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value, parameter);
        var normalized = value.Trim();
        if (normalized.Length < minimumLength || normalized.Length > maximumLength)
            throw new ArgumentException($"{parameter} must be between {minimumLength} and {maximumLength} characters.", parameter);
        return normalized;
    }

    public static string? NormalizeOptional(string? value, string parameter, int maximumLength)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        var normalized = value.Trim();
        if (normalized.Length > maximumLength)
            throw new ArgumentException($"{parameter} cannot exceed {maximumLength} characters.", parameter);
        return normalized;
    }

    public static string NormalizeSlug(string value)
    {
        var normalized = NormalizeRequired(value, nameof(value), 160, 3);
        if (!normalized.Equals(normalized.ToLowerInvariant(), StringComparison.Ordinal)
            || !SlugPattern().IsMatch(normalized))
            throw new ArgumentException("Slug must contain lowercase letters, numbers, and single hyphens.", nameof(value));
        return normalized;
    }

    [GeneratedRegex("^[a-z0-9]+(?:-[a-z0-9]+)*$")]
    private static partial Regex SlugPattern();
}
