using DevRecall.Application.Common.Exceptions;
using DevRecall.Application.Identity;
using DevRecall.Domain.Interview.FollowUps;

namespace DevRecall.Application.Interview.FollowUps;

public sealed class UpdateInterviewFollowUpHandler(
    IInterviewQuestionRepository questionRepository,
    IInterviewFollowUpQuestionRepository followUpRepository,
    ICurrentUser currentUser)
{
    public async Task<InterviewFollowUpResponseData> HandleAsync(
        Guid questionId, Guid followUpId, string prompt,
        CancellationToken cancellationToken)
    {
        InterviewFollowUpSupport.ValidatePrompt(prompt);
        var question = await InterviewFollowUpSupport.GetOwnedQuestionAsync(
            questionId, questionRepository, currentUser,
            requireActive: true, cancellationToken);
        var followUp = await followUpRepository.GetByIdAndQuestionIdAsync(
            followUpId, question.Id, cancellationToken);

        if (followUp is null)
        {
            throw new NotFoundException(
                InterviewFollowUpQuestionErrors.NotFound.Code,
                InterviewFollowUpQuestionErrors.NotFound.Message);
        }

        if (followUp.Status == InterviewFollowUpQuestionStatus.Archived)
        {
            throw new ConflictException(
                InterviewFollowUpQuestionErrors.Archived.Code,
                InterviewFollowUpQuestionErrors.Archived.Message);
        }

        if (followUp.UpdatePrompt(prompt, DateTimeOffset.UtcNow))
        {
            await followUpRepository.SaveChangesAsync(cancellationToken);
        }

        return InterviewFollowUpSupport.Map(followUp);
    }
}
