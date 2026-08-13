using DevRecall.Application.Common.Exceptions;
using DevRecall.Domain.Reviews;

namespace DevRecall.Application.Reviews;

internal static class ReviewResourceTypeParser
{
    public static ReviewResourceType Parse(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ValidationException(
                new Dictionary<string, string[]>
                {
                    ["resourceType"] = ["Resource type is required."]
                });
        }

        var normalized = value.Trim()
            .Replace(" ", string.Empty)
            .Replace("-", string.Empty)
            .Replace("_", string.Empty);
        if (int.TryParse(normalized, out _))
        {
            throw InvalidResourceType();
        }

        var parsed = Enum.TryParse<ReviewResourceType>(
            normalized, true, out var resourceType);

        if (!parsed || !Enum.IsDefined(resourceType))
        {
            throw InvalidResourceType();
        }

        return resourceType;
    }

    private static ValidationException InvalidResourceType() =>
        new(new Dictionary<string, string[]>
        {
            ["resourceType"] =
            [
                "Resource type must be KnowledgeNode, InterviewQuestion, DsaProblem, or LearningContent."
            ]
        });
}
