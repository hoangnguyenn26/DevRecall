using DevRecall.Application.Common.Exceptions;
using DevRecall.Domain.Interview;

namespace DevRecall.Application.Interview;

internal static class InterviewQuestionDifficultyParser
{
    public static InterviewQuestionDifficulty Parse(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw CreateValidationException("Difficulty is required.");
        }

        var normalized = value.Trim();
        if (int.TryParse(normalized, out _))
        {
            throw CreateValidationException(
                "Difficulty must be Easy, Medium, or Hard.");
        }

        var parsed = Enum.TryParse<InterviewQuestionDifficulty>(
            normalized, ignoreCase: true, out var difficulty);

        if (!parsed || !Enum.IsDefined(difficulty))
        {
            throw CreateValidationException(
                "Difficulty must be Easy, Medium, or Hard.");
        }

        return difficulty;
    }

    public static InterviewQuestionDifficulty? ParseOptional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : Parse(value);

    private static ValidationException CreateValidationException(
        string message) =>
        new(new Dictionary<string, string[]>
        {
            ["difficulty"] = [message]
        });
}
