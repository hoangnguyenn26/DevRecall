using DevRecall.Application.Common.Exceptions;
using DevRecall.Application.Identity;
using DevRecall.Domain.Interview.FollowUps;

namespace DevRecall.Application.Interview.FollowUps;

public sealed class ChangeInterviewFollowUpOrderHandler(
    IInterviewQuestionRepository questionRepository,
    IInterviewFollowUpQuestionRepository followUpRepository,
    ICurrentUser currentUser)
{
    public async Task HandleAsync(
        Guid questionId, Guid followUpId, int targetIndex,
        CancellationToken cancellationToken)
    {
        var question = await InterviewFollowUpSupport.GetOwnedQuestionAsync(
            questionId, questionRepository, currentUser,
            requireActive: true, cancellationToken);
        var followUps = (await followUpRepository.GetActiveByQuestionIdAsync(
            question.Id, trackChanges: true, cancellationToken)).ToList();
        var currentIndex = followUps.FindIndex(item => item.Id == followUpId);

        if (currentIndex < 0)
        {
            throw new NotFoundException(
                InterviewFollowUpQuestionErrors.NotFound.Code,
                InterviewFollowUpQuestionErrors.NotFound.Message);
        }

        if (targetIndex < 0 || targetIndex >= followUps.Count)
        {
            throw new ValidationException(
                new Dictionary<string, string[]>
                {
                    ["targetIndex"] =
                    [
                        "Target index must refer to an existing follow-up position."
                    ]
                });
        }

        if (currentIndex == targetIndex)
        {
            return;
        }

        var moved = followUps[currentIndex];
        followUps.RemoveAt(currentIndex);
        followUps.Insert(targetIndex, moved);
        var now = DateTimeOffset.UtcNow;

        for (var index = 0; index < followUps.Count; index++)
        {
            followUps[index].ChangeSortOrder(index, now);
        }

        await followUpRepository.SaveChangesAsync(cancellationToken);
    }
}
