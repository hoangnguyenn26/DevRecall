using DevRecall.Application.Common.Exceptions;
using DevRecall.Domain.Interview;

namespace DevRecall.Application.Interview;

internal static class InterviewQuestionInputValidator
{
    public static void Validate(
        string? title, string? question, string? topic, string? notes)
    {
        var errors = new Dictionary<string, string[]>();
        ValidateRequired(
            title, "title", InterviewQuestionText.TitleMaxLength, errors);
        ValidateRequired(
            question, "question", InterviewQuestionText.QuestionMaxLength, errors);
        ValidateRequired(
            topic, "topic", InterviewQuestionText.TopicMaxLength, errors);

        if (notes?.Trim().Length > InterviewQuestionText.NotesMaxLength)
        {
            errors["notes"] =
                [$"Notes cannot exceed {InterviewQuestionText.NotesMaxLength} characters."];
        }

        if (errors.Count > 0)
        {
            throw new ValidationException(errors);
        }
    }

    private static void ValidateRequired(
        string? value, string fieldName, int maxLength,
        Dictionary<string, string[]> errors)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            errors[fieldName] =
                [$"{char.ToUpperInvariant(fieldName[0])}{fieldName[1..]} is required."];
            return;
        }

        var normalized = string.Join(
            ' ',
            value.Split(
                (char[]?)null,
                StringSplitOptions.RemoveEmptyEntries));

        if (normalized.Length > maxLength)
        {
            errors[fieldName] =
                [$"{char.ToUpperInvariant(fieldName[0])}{fieldName[1..]} cannot exceed {maxLength} characters."];
        }
    }
}
