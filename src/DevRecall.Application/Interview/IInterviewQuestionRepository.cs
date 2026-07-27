using DevRecall.Application.Common.Pagination;
using DevRecall.Domain.Interview;

namespace DevRecall.Application.Interview;

public interface IInterviewQuestionRepository
{
    Task<InterviewQuestion?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken);

    Task<InterviewQuestion?> GetByIdAndUserIdAsync(
        Guid id,
        Guid userId,
        CancellationToken cancellationToken);

    Task<PagedReadResult<InterviewQuestionListReadItem>> GetActiveListAsync(
        Guid userId,
        string? topic,
        InterviewQuestionDifficulty? difficulty,
        int skip,
        int take,
        CancellationToken cancellationToken);

    void Add(InterviewQuestion question);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
