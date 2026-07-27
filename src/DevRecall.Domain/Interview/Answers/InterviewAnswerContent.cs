namespace DevRecall.Domain.Interview.Answers;

public static class InterviewAnswerContent
{
    public const int MaxLength = 50_000;

    public static string Normalize(string? content)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            throw new ArgumentException(
                "Answer content is required.",
                nameof(content));
        }

        var normalizedContent = content.Trim();

        if (normalizedContent.Length > MaxLength)
        {
            throw new ArgumentException(
                $"Answer content cannot exceed {MaxLength} characters.",
                nameof(content));
        }

        return normalizedContent;
    }
}
