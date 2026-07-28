using DevRecall.Application.Common.Exceptions;
using DevRecall.Domain.Reviews;

namespace DevRecall.Application.Reviews;

internal static class ReviewEvaluationParser
{
    public static ReviewEvaluation Parse(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ValidationException(
                new Dictionary<string, string[]>
                {
                    ["evaluation"] = ["Evaluation is required."]
                });
        }

        var parsed = Enum.TryParse<ReviewEvaluation>(
            value.Trim(), true, out var evaluation);
        if (!parsed || !Enum.IsDefined(evaluation))
        {
            throw new ValidationException(
                new Dictionary<string, string[]>
                {
                    ["evaluation"] =
                        ["Evaluation must be Again, Hard, Good, or Easy."]
                });
        }

        return evaluation;
    }
}
