using DevRecall.Application.Common.Pagination;
using DevRecall.Domain.Dsa.Attempts;

namespace DevRecall.Application.Dsa.Attempts;

public interface IDsaAttemptRepository
{
    Task<(DsaPracticeSubmission Submission, DsaAttempt Attempt)?> GetPracticeSubmissionAsync(
        Guid userId, Guid submissionId, CancellationToken cancellationToken);
    Task<DsaPracticeSubmission?> GetPracticeSubmissionByAttemptAsync(
        Guid userId, Guid dsaProblemId, Guid attemptId, CancellationToken cancellationToken);

    Task<DsaAttempt?> GetByIdAndProblemIdAsync(
        Guid id, Guid dsaProblemId, CancellationToken cancellationToken);

    Task<int> GetNextAttemptNumberAsync(
        Guid dsaProblemId, CancellationToken cancellationToken);

    Task<PagedReadResult<DsaAttemptListReadItem>> GetListAsync(
        Guid dsaProblemId, DsaAttemptResult? result, int skip, int take,
        CancellationToken cancellationToken);

    Task<DsaAttempt?> GetLatestSuccessfulAsync(
        Guid dsaProblemId, CancellationToken cancellationToken);

    Task<IReadOnlyList<DsaAttempt>> GetByIdsAndProblemIdAsync(
        Guid dsaProblemId, IReadOnlyCollection<Guid> attemptIds,
        CancellationToken cancellationToken);

    void Add(DsaAttempt attempt);

    void AddPracticeSubmission(DsaPracticeSubmission submission);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
