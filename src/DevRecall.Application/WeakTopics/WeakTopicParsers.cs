using DevRecall.Application.Common.Exceptions;
using DevRecall.Domain.WeakTopics;

namespace DevRecall.Application.WeakTopics;

public static class WeakTopicResourceTypeParser
{
    public static WeakTopicResourceType Parse(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw Invalid("resourceType", "Resource type is required.");
        }

        var normalized = value.Trim().Replace(" ", string.Empty)
            .Replace("-", string.Empty).Replace("_", string.Empty);
        if (normalized.All(char.IsDigit)
            || !Enum.TryParse<WeakTopicResourceType>(normalized, true, out var type)
            || !Enum.IsDefined(type))
        {
            throw Invalid("resourceType",
                "Resource type must be KnowledgeNode, InterviewQuestion, or DsaProblem.");
        }

        return type;
    }

    internal static ValidationException Invalid(string field, string message) =>
        new(new Dictionary<string, string[]> { [field] = [message] });
}

public static class WeaknessLevelParser
{
    public static WeaknessLevel Parse(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw WeakTopicResourceTypeParser.Invalid(
                "level", "Weakness level is required.");
        }

        var normalized = value.Trim().Replace(" ", string.Empty)
            .Replace("-", string.Empty).Replace("_", string.Empty);
        if (normalized.All(char.IsDigit)
            || !Enum.TryParse<WeaknessLevel>(normalized, true, out var level)
            || !Enum.IsDefined(level))
        {
            throw WeakTopicResourceTypeParser.Invalid(
                "level", "Level must be None, Low, Medium, High, or Critical.");
        }

        return level;
    }
}
