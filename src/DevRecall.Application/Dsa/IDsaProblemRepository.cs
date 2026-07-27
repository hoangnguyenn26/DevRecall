using DevRecall.Application.Common.Pagination;
using DevRecall.Domain.Dsa;

namespace DevRecall.Application.Dsa;

public interface IDsaProblemRepository
{
    Task<DsaProblem?> GetByIdAsync(
        Guid id, CancellationToken cancellationToken);

    Task<DsaProblem?> GetByIdAndUserIdAsync(
        Guid id, Guid userId, CancellationToken cancellationToken);

    Task<PagedReadResult<DsaProblemListReadItem>> GetActiveListAsync(
        Guid userId, DsaProblemDifficulty? difficulty,
        string? normalizedTopic, string? source, int skip, int take,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<DsaProblemTopicReadItem>> GetTopicsByProblemIdsAsync(
        IReadOnlyCollection<Guid> problemIds,
        CancellationToken cancellationToken);

    void Add(DsaProblem problem);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
