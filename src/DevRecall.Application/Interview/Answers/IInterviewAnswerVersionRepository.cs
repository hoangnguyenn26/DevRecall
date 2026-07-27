using DevRecall.Domain.Interview.Answers;

namespace DevRecall.Application.Interview.Answers;

public interface IInterviewAnswerVersionRepository
{
    Task<InterviewAnswerVersion?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken);

    Task<InterviewAnswerVersion?> GetByIdAndQuestionIdAsync(
        Guid id,
        Guid interviewQuestionId,
        CancellationToken cancellationToken);

    Task<bool> HasDraftAsync(
        Guid interviewQuestionId,
        CancellationToken cancellationToken);

    Task<int> GetNextVersionNumberAsync(
        Guid interviewQuestionId,
        CancellationToken cancellationToken);

    Task<InterviewAnswerVersion?> GetCurrentPublishedAsync(
        Guid interviewQuestionId,
        CancellationToken cancellationToken);

    void Add(InterviewAnswerVersion answerVersion);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
