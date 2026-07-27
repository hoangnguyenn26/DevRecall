using DevRecall.Application.Common.Exceptions;
using DevRecall.Domain.Interview.Answers;

namespace DevRecall.Application.Interview.Answers;

internal static class InterviewAnswerInputValidator
{
    public static void ValidateContent(string? content)
    {
        var errors = new Dictionary<string, string[]>();

        if (string.IsNullOrWhiteSpace(content))
        {
            errors["content"] = ["Answer content is required."];
        }
        else if (content.Trim().Length > InterviewAnswerContent.MaxLength)
        {
            errors["content"] =
            [
                $"Answer content cannot exceed {InterviewAnswerContent.MaxLength} characters."
            ];
        }

        if (errors.Count > 0)
        {
            throw new ValidationException(errors);
        }
    }
}
