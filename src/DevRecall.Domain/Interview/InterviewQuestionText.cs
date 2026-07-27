namespace DevRecall.Domain.Interview;

public static class InterviewQuestionText
{
    public const int TitleMaxLength = 200;
    public const int QuestionMaxLength = 5_000;
    public const int TopicMaxLength = 100;
    public const int NotesMaxLength = 10_000;

    public static string NormalizeRequired(
        string? value, string parameterName, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException(
                $"{parameterName} is required.",
                parameterName);
        }

        var normalized = string.Join(
            ' ',
            value.Split(
                (char[]?)null,
                StringSplitOptions.RemoveEmptyEntries));

        if (normalized.Length > maxLength)
        {
            throw new ArgumentException(
                $"{parameterName} cannot exceed {maxLength} characters.",
                parameterName);
        }

        return normalized;
    }

    public static string? NormalizeOptional(
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
