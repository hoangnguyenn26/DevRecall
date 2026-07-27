using DevRecall.Application.Common.Exceptions;
using DevRecall.Domain.Dsa;

namespace DevRecall.Application.Dsa;

internal static class DsaProblemInputValidator
{
    public static void Validate(
        string? title, string? description, string? source,
        string? externalUrl, IReadOnlyCollection<string>? topics)
    {
        var errors = new Dictionary<string, string[]>();
        ValidateRequiredSingleLine(
            title, "title", DsaProblemText.TitleMaxLength, errors);
        ValidateRequiredMultiline(
            description, "description",
            DsaProblemText.DescriptionMaxLength, errors);
        ValidateOptionalSingleLine(
            source, "source", DsaProblemText.SourceMaxLength, errors);
        ValidateExternalUrl(externalUrl, errors);
        ValidateTopics(topics, errors);
        if (errors.Count > 0)
        {
            throw new ValidationException(errors);
        }
    }

    private static void ValidateRequiredSingleLine(
        string? value, string fieldName, int maxLength,
        Dictionary<string, string[]> errors)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            errors[fieldName] = [$"{fieldName} is required."];
            return;
        }

        if (NormalizeSingleLine(value).Length > maxLength)
        {
            errors[fieldName] =
                [$"{fieldName} cannot exceed {maxLength} characters."];
        }
    }

    private static void ValidateRequiredMultiline(
        string? value, string fieldName, int maxLength,
        Dictionary<string, string[]> errors)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            errors[fieldName] = [$"{fieldName} is required."];
            return;
        }

        if (value.Trim().Length > maxLength)
        {
            errors[fieldName] =
                [$"{fieldName} cannot exceed {maxLength} characters."];
        }
    }

    private static void ValidateOptionalSingleLine(
        string? value, string fieldName, int maxLength,
        Dictionary<string, string[]> errors)
    {
        if (!string.IsNullOrWhiteSpace(value)
            && NormalizeSingleLine(value).Length > maxLength)
        {
            errors[fieldName] =
                [$"{fieldName} cannot exceed {maxLength} characters."];
        }
    }

    private static void ValidateExternalUrl(
        string? externalUrl, Dictionary<string, string[]> errors)
    {
        if (string.IsNullOrWhiteSpace(externalUrl))
        {
            return;
        }

        var normalized = externalUrl.Trim();
        if (normalized.Length > DsaProblemText.ExternalUrlMaxLength)
        {
            errors["externalUrl"] =
            [
                $"External URL cannot exceed {DsaProblemText.ExternalUrlMaxLength} characters."
            ];
            return;
        }

        if (!Uri.TryCreate(normalized, UriKind.Absolute, out var uri)
            || (uri.Scheme != Uri.UriSchemeHttp
                && uri.Scheme != Uri.UriSchemeHttps))
        {
            errors["externalUrl"] =
                ["External URL must be a valid HTTP or HTTPS URL."];
        }
    }

    private static void ValidateTopics(
        IReadOnlyCollection<string>? topics,
        Dictionary<string, string[]> errors)
    {
        if (topics is null)
        {
            return;
        }

        if (topics.Count > DsaProblemTopic.MaximumTopicsPerProblem)
        {
            errors["topics"] =
            [
                $"A DSA problem cannot have more than {DsaProblemTopic.MaximumTopicsPerProblem} topics."
            ];
            return;
        }

        if (topics.Any(topic => !string.IsNullOrWhiteSpace(topic)
            && NormalizeSingleLine(topic).Length > DsaProblemTopic.MaxLength))
        {
            errors["topics"] =
                [$"Each topic cannot exceed {DsaProblemTopic.MaxLength} characters."];
        }
    }

    internal static string NormalizeSingleLine(string value) =>
        string.Join(' ', value.Split(
            (char[]?)null, StringSplitOptions.RemoveEmptyEntries));
}
