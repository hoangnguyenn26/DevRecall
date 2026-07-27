using DevRecall.Application.Common.Exceptions;
using DevRecall.Application.Identity;
using DevRecall.Domain.Interview;

namespace DevRecall.Application.Interview.FollowUps;

public sealed class GetInterviewFollowUpsHandler(
    IInterviewQuestionRepository questionRepository,
    IInterviewFollowUpQuestionRepository followUpRepository,
    ICurrentUser currentUser)
{
    public async Task<IReadOnlyList<InterviewFollowUpResponseData>> HandleAsync(
        Guid questionId, CancellationToken cancellationToken)
    {
        var userId = InterviewHandlerSupport.GetCurrentUserId(currentUser);
        var question = await questionRepository.GetByIdAndUserIdAsync(
            questionId, userId, cancellationToken);

        if (question is null)
        {
            throw new NotFoundException(
                InterviewQuestionErrors.NotFound.Code,
                InterviewQuestionErrors.NotFound.Message);
        }

        var followUps = await followUpRepository.GetActiveByQuestionIdAsync(
            question.Id, trackChanges: false, cancellationToken);
        return followUps.Select(InterviewFollowUpSupport.Map).ToList();
    }
}
