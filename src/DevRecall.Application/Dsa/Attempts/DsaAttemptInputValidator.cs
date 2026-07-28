using DevRecall.Application.Common.Exceptions;
using DevRecall.Domain.Dsa.Attempts;

namespace DevRecall.Application.Dsa.Attempts;

internal static class DsaAttemptInputValidator
{
    public static void Validate(
        string? language, string? solutionCode, string? approach,
        string? timeComplexity, string? spaceComplexity, int durationMinutes,
        string? notes, DateTimeOffset attemptedAtUtc)
    {
        var errors = new Dictionary<string, string[]>();
        ValidateOptionalSingleLine(
            language, "language", DsaAttemptText.LanguageMaxLength, errors);
        ValidateOptionalMultiline(
            solutionCode, "solutionCode",
            DsaAttemptText.SolutionCodeMaxLength, errors);
        ValidateOptionalMultiline(
            approach, "approach", DsaAttemptText.ApproachMaxLength, errors);
        ValidateOptionalSingleLine(
            timeComplexity, "timeComplexity",
            DsaAttemptText.ComplexityMaxLength, errors);
        ValidateOptionalSingleLine(
            spaceComplexity, "spaceComplexity",
            DsaAttemptText.ComplexityMaxLength, errors);
        ValidateOptionalMultiline(
            notes, "notes", DsaAttemptText.NotesMaxLength, errors);
        if (durationMinutes < 0
            || durationMinutes > DsaAttemptText.MaximumDurationMinutes)
        {
            errors["durationMinutes"] =
            [
                $"Duration must be between 0 and {DsaAttemptText.MaximumDurationMinutes} minutes."
            ];
        }

        if (attemptedAtUtc.Offset != TimeSpan.Zero)
        {
            errors["attemptedAtUtc"] = ["AttemptedAtUtc must be in UTC."];
        }

        if (errors.Count > 0)
        {
            throw new ValidationException(errors);
        }
    }

    private static void ValidateOptionalSingleLine(
        string? value, string fieldName, int maxLength,
        Dictionary<string, string[]> errors)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return;
        }

        var normalized = string.Join(' ', value.Split(
            (char[]?)null, StringSplitOptions.RemoveEmptyEntries));
        if (normalized.Length > maxLength)
        {
            errors[fieldName] =
                [$"{fieldName} cannot exceed {maxLength} characters."];
        }
    }

    private static void ValidateOptionalMultiline(
        string? value, string fieldName, int maxLength,
        Dictionary<string, string[]> errors)
    {
        if (!string.IsNullOrWhiteSpace(value)
            && value.Trim().Length > maxLength)
        {
            errors[fieldName] =
                [$"{fieldName} cannot exceed {maxLength} characters."];
        }
    }
}
