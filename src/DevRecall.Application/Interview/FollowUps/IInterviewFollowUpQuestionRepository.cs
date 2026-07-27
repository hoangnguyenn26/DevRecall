using DevRecall.Domain.Interview.FollowUps;

namespace DevRecall.Application.Interview.FollowUps;

public interface IInterviewFollowUpQuestionRepository
{
    Task<InterviewFollowUpQuestion?> GetByIdAndQuestionIdAsync(
        Guid id, Guid interviewQuestionId, CancellationToken cancellationToken);

    Task<IReadOnlyList<InterviewFollowUpQuestion>> GetActiveByQuestionIdAsync(
        Guid interviewQuestionId, bool trackChanges,
        CancellationToken cancellationToken);

    Task<int> CountActiveAsync(
        Guid interviewQuestionId, CancellationToken cancellationToken);

    void Add(InterviewFollowUpQuestion followUp);
    Task SaveChangesAsync(CancellationToken cancellationToken);
}
