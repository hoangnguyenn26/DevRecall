using DevRecall.Application.Identity;
using DevRecall.Domain.Interview.FollowUps;

namespace DevRecall.Application.Interview.FollowUps;

public sealed class CreateInterviewFollowUpHandler(
    IInterviewQuestionRepository questionRepository,
    IInterviewFollowUpQuestionRepository followUpRepository,
    ICurrentUser currentUser)
{
    public async Task<InterviewFollowUpResponseData> HandleAsync(
        Guid questionId, string prompt, CancellationToken cancellationToken)
    {
        InterviewFollowUpSupport.ValidatePrompt(prompt);
        var question = await InterviewFollowUpSupport.GetOwnedQuestionAsync(
            questionId, questionRepository, currentUser,
            requireActive: true, cancellationToken);
        var sortOrder = await followUpRepository.CountActiveAsync(
            question.Id, cancellationToken);
        var followUp = InterviewFollowUpQuestion.Create(
            Guid.NewGuid(), question.Id, prompt, sortOrder,
            DateTimeOffset.UtcNow);

        followUpRepository.Add(followUp);
        await followUpRepository.SaveChangesAsync(cancellationToken);
        return InterviewFollowUpSupport.Map(followUp);
    }
}
