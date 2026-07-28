namespace DevRecall.Infrastructure.Reviews.Resources;

internal static class ReviewPreviewBuilder
{
    public static string? Build(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        const int maximumLength = 240;
        var normalized = string.Join(
            ' ', value.Split(
                (char[]?)null, StringSplitOptions.RemoveEmptyEntries));
        return normalized.Length <= maximumLength
            ? normalized
            : normalized[..maximumLength] + "…";
    }
}
