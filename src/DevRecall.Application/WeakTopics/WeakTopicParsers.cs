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
        return normalized.ToLowerInvariant() switch
        {
            "knowledgenode" => WeakTopicResourceType.KnowledgeNode,
            "interviewquestion" => WeakTopicResourceType.InterviewQuestion,
            "dsaproblem" => WeakTopicResourceType.DsaProblem,
            _ => throw Invalid(
                "resourceType",
                "Resource type must be KnowledgeNode, InterviewQuestion, or DsaProblem.")
        };
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
        return normalized.ToLowerInvariant() switch
        {
            "none" => WeaknessLevel.None,
            "low" => WeaknessLevel.Low,
            "medium" => WeaknessLevel.Medium,
            "high" => WeaknessLevel.High,
            "critical" => WeaknessLevel.Critical,
            _ => throw WeakTopicResourceTypeParser.Invalid(
                "level", "Level must be None, Low, Medium, High, or Critical.")
        };
    }
}
