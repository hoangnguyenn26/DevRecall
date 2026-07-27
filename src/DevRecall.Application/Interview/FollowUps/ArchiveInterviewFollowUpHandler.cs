using DevRecall.Application.Common.Exceptions;
using DevRecall.Application.Identity;
using DevRecall.Domain.Interview.FollowUps;

namespace DevRecall.Application.Interview.FollowUps;

public sealed class ArchiveInterviewFollowUpHandler(
    IInterviewQuestionRepository questionRepository,
    IInterviewFollowUpQuestionRepository followUpRepository,
    ICurrentUser currentUser)
{
    public async Task HandleAsync(
        Guid questionId, Guid followUpId, CancellationToken cancellationToken)
    {
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

        var now = DateTimeOffset.UtcNow;

        if (!followUp.Archive(now))
        {
            return;
        }

        var active = (await followUpRepository.GetActiveByQuestionIdAsync(
            question.Id, trackChanges: true, cancellationToken))
            .Where(item => item.Id != followUp.Id)
            .ToList();

        for (var index = 0; index < active.Count; index++)
        {
            active[index].ChangeSortOrder(index, now);
        }

        await followUpRepository.SaveChangesAsync(cancellationToken);
    }
}
