using DevRecall.Domain.Dsa.Attempts;

namespace DevRecall.Application.Dsa.Attempts;

public interface IDsaAttemptRepository
{
    Task<DsaAttempt?> GetByIdAndProblemIdAsync(
        Guid id, Guid dsaProblemId, CancellationToken cancellationToken);

    Task<int> GetNextAttemptNumberAsync(
        Guid dsaProblemId, CancellationToken cancellationToken);

    void Add(DsaAttempt attempt);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
