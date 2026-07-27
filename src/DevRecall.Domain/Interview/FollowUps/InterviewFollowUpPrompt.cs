namespace DevRecall.Domain.Interview.FollowUps;

public static class InterviewFollowUpPrompt
{
    public const int MaxLength = 2_000;

    public static string Normalize(string? prompt)
    {
        if (string.IsNullOrWhiteSpace(prompt))
        {
            throw new ArgumentException(
                "Follow-up prompt is required.", nameof(prompt));
        }

        var normalized = string.Join(
            ' ', prompt.Split(
                (char[]?)null, StringSplitOptions.RemoveEmptyEntries));

        if (normalized.Length > MaxLength)
        {
            throw new ArgumentException(
                $"Follow-up prompt cannot exceed {MaxLength} characters.",
                nameof(prompt));
        }

        return normalized;
    }
}
