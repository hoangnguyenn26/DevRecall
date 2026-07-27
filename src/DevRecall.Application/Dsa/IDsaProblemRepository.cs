using DevRecall.Domain.Dsa;

namespace DevRecall.Application.Dsa;

public interface IDsaProblemRepository
{
    Task<DsaProblem?> GetByIdAsync(
        Guid id, CancellationToken cancellationToken);

    Task<DsaProblem?> GetByIdAndUserIdAsync(
        Guid id, Guid userId, CancellationToken cancellationToken);

    void Add(DsaProblem problem);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
