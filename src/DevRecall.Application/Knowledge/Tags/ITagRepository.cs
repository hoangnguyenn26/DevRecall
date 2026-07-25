using DevRecall.Domain.Knowledge.Tags;

namespace DevRecall.Application.Knowledge.Tags;

public interface ITagRepository
{
    Task<bool> ExistsByNormalizedNameAsync(Guid userId, string normalizedName,
        Guid? excludedTagId, CancellationToken cancellationToken);

    Task<Tag?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<IReadOnlyList<Tag>> GetActiveByUserIdAsync(
        Guid userId,
        CancellationToken cancellationToken);

    void Add(Tag tag);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
