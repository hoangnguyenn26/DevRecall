using DevRecall.Application.Knowledge.Tags;
using DevRecall.Domain.Knowledge.Tags;
using DevRecall.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace DevRecall.Infrastructure.Knowledge.Tags;

internal sealed class TagRepository(DevRecallDbContext dbContext)
    : ITagRepository
{
    public Task<bool> ExistsByNormalizedNameAsync(
        Guid userId,
        string normalizedName,
        Guid? excludedTagId,
        CancellationToken cancellationToken)
    {
        return dbContext.Tags.AnyAsync(
            tag => tag.UserId == userId
                && tag.NormalizedName == normalizedName
                && (excludedTagId == null || tag.Id != excludedTagId.Value),
            cancellationToken);
    }

    public Task<Tag?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        dbContext.Tags.SingleOrDefaultAsync(tag => tag.Id == id, cancellationToken);

    public async Task<IReadOnlyList<Tag>> GetActiveByUserIdAsync(
        Guid userId,
        CancellationToken cancellationToken)
    {
        return await dbContext.Tags
            .AsNoTracking()
            .Where(tag => tag.UserId == userId && tag.Status == TagStatus.Active)
            .OrderBy(tag => tag.Name)
            .ThenBy(tag => tag.Id)
            .ToListAsync(cancellationToken);
    }

    public Task<int> CountActiveByIdsAsync(
        Guid userId,
        IReadOnlyCollection<Guid> tagIds,
        CancellationToken cancellationToken) =>
        dbContext.Tags.CountAsync(
            tag => tag.UserId == userId
                && tag.Status == TagStatus.Active
                && tagIds.Contains(tag.Id),
            cancellationToken);

    public void Add(Tag tag) => dbContext.Tags.Add(tag);

    public Task SaveChangesAsync(CancellationToken cancellationToken) =>
        dbContext.SaveChangesAsync(cancellationToken);
}
