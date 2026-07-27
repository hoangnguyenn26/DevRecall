using DevRecall.Application.Common.Exceptions;
using DevRecall.Application.Identity;
using DevRecall.Domain.Interview;
using DevRecall.Domain.Interview.FollowUps;

namespace DevRecall.Application.Interview.FollowUps;

internal static class InterviewFollowUpSupport
{
    public static void ValidatePrompt(string? prompt)
    {
        var normalizedLength = string.IsNullOrWhiteSpace(prompt)
            ? 0
            : string.Join(
                ' ', prompt.Split(
                    (char[]?)null,
                    StringSplitOptions.RemoveEmptyEntries)).Length;

        if (normalizedLength == 0)
        {
            throw new ValidationException(
                new Dictionary<string, string[]>
                {
                    ["prompt"] = ["Follow-up prompt is required."]
                });
        }

        if (normalizedLength > InterviewFollowUpPrompt.MaxLength)
        {
            throw new ValidationException(
                new Dictionary<string, string[]>
                {
                    ["prompt"] =
                    [
                        $"Follow-up prompt cannot exceed {InterviewFollowUpPrompt.MaxLength} characters."
                    ]
                });
        }
    }

    public static async Task<InterviewQuestion> GetOwnedQuestionAsync(
        Guid questionId,
        IInterviewQuestionRepository repository,
        ICurrentUser currentUser,
        bool requireActive,
        CancellationToken cancellationToken)
    {
        var userId = InterviewHandlerSupport.GetCurrentUserId(currentUser);
        var question = await repository.GetByIdAsync(
            questionId, cancellationToken);

        if (question is null || question.UserId != userId)
        {
            throw new NotFoundException(
                InterviewQuestionErrors.NotFound.Code,
                InterviewQuestionErrors.NotFound.Message);
        }

        if (requireActive
            && question.Status == InterviewQuestionStatus.Archived)
        {
            throw new ConflictException(
                InterviewQuestionErrors.Archived.Code,
                InterviewQuestionErrors.Archived.Message);
        }

        return question;
    }

    public static InterviewFollowUpResponseData Map(
        InterviewFollowUpQuestion followUp) =>
        new(
            followUp.Id, followUp.InterviewQuestionId, followUp.Prompt,
            followUp.SortOrder, followUp.Status.ToString(),
            followUp.CreatedAtUtc, followUp.UpdatedAtUtc);
}

public sealed record InterviewFollowUpResponseData(
    Guid Id,
    Guid InterviewQuestionId,
    string Prompt,
    int SortOrder,
    string Status,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc);
