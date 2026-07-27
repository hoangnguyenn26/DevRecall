namespace DevRecall.Domain.Dsa;

public static class DsaProblemText
{
    public const int TitleMaxLength = 200;
    public const int DescriptionMaxLength = 20_000;
    public const int SourceMaxLength = 100;
    public const int ExternalUrlMaxLength = 2_000;

    public static string NormalizeRequiredSingleLine(
        string? value, string parameterName, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException(
                $"{parameterName} is required.", parameterName);
        }

        var normalized = NormalizeWhitespace(value);
        if (normalized.Length > maxLength)
        {
            throw new ArgumentException(
                $"{parameterName} cannot exceed {maxLength} characters.",
                parameterName);
        }

        return normalized;
    }

    public static string NormalizeRequiredMultiline(
        string? value, string parameterName, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException(
                $"{parameterName} is required.", parameterName);
        }

        var normalized = value.Trim();
        if (normalized.Length > maxLength)
        {
            throw new ArgumentException(
                $"{parameterName} cannot exceed {maxLength} characters.",
                parameterName);
        }

        return normalized;
    }

    public static string? NormalizeOptionalSingleLine(
        string? value, string parameterName, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var normalized = NormalizeWhitespace(value);
        if (normalized.Length > maxLength)
        {
            throw new ArgumentException(
                $"{parameterName} cannot exceed {maxLength} characters.",
                parameterName);
        }

        return normalized;
    }

    private static string NormalizeWhitespace(string value) =>
        string.Join(' ', value.Split(
            (char[]?)null, StringSplitOptions.RemoveEmptyEntries));
}
