namespace DevRecall.Domain.Dsa.Attempts;

public static class DsaAttemptText
{
    public const int LanguageMaxLength = 50;
    public const int SolutionCodeMaxLength = 100_000;
    public const int ApproachMaxLength = 20_000;
    public const int ComplexityMaxLength = 100;
    public const int NotesMaxLength = 20_000;
    public const int MaximumDurationMinutes = 24 * 60;

    public static string? NormalizeOptionalSingleLine(
        string? value, string parameterName, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var normalized = string.Join(' ', value.Split(
            (char[]?)null, StringSplitOptions.RemoveEmptyEntries));
        if (normalized.Length > maxLength)
        {
            throw new ArgumentException(
                $"{parameterName} cannot exceed {maxLength} characters.",
                parameterName);
        }

        return normalized;
    }

    public static string? NormalizeOptionalMultiline(
        string? value, string parameterName, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
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
}
