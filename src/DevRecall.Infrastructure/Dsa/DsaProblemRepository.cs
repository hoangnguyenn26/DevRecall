using DevRecall.Application.Common.Pagination;
using DevRecall.Application.Dsa;
using DevRecall.Domain.Dsa;
using DevRecall.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace DevRecall.Infrastructure.Dsa;

internal sealed class DsaProblemRepository(DevRecallDbContext dbContext)
    : IDsaProblemRepository
{
    public Task<DsaProblem?> GetByIdAsync(
        Guid id, CancellationToken cancellationToken) =>
        dbContext.DsaProblems
            .Include(problem => problem.Topics)
            .SingleOrDefaultAsync(
                problem => problem.Id == id, cancellationToken);

    public Task<DsaProblem?> GetByIdAndUserIdAsync(
        Guid id, Guid userId, CancellationToken cancellationToken) =>
        dbContext.DsaProblems
            .AsNoTracking()
            .Include(problem => problem.Topics)
            .SingleOrDefaultAsync(
                problem => problem.Id == id && problem.UserId == userId,
                cancellationToken);

    public async Task<PagedReadResult<DsaProblemListReadItem>>
        GetActiveListAsync(
            Guid userId, DsaProblemDifficulty? difficulty,
            string? normalizedTopic, string? source, int skip, int take,
            CancellationToken cancellationToken)
    {
        var query = dbContext.DsaProblems
            .AsNoTracking()
            .Where(problem => problem.UserId == userId
                && problem.Status == DsaProblemStatus.Active);
        if (difficulty is not null)
        {
            query = query.Where(problem =>
                problem.Difficulty == difficulty.Value);
        }

        if (normalizedTopic is not null)
        {
            query = query.Where(problem => problem.Topics.Any(topic =>
                topic.NormalizedName == normalizedTopic));
        }

        if (source is not null)
        {
            var escapedSource = source
                .Replace("\\", "\\\\", StringComparison.Ordinal)
                .Replace("%", "\\%", StringComparison.Ordinal)
                .Replace("_", "\\_", StringComparison.Ordinal);
            query = query.Where(problem => problem.Source != null
                && EF.Functions.ILike(problem.Source, escapedSource, "\\"));
        }

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(problem => problem.UpdatedAtUtc)
            .ThenBy(problem => problem.Title)
            .ThenBy(problem => problem.Id)
            .Skip(skip)
            .Take(take)
            .Select(problem => new DsaProblemListReadItem(
                problem.Id, problem.Title, problem.Difficulty,
                problem.Source, problem.UpdatedAtUtc))
            .ToListAsync(cancellationToken);
        return new PagedReadResult<DsaProblemListReadItem>(
            items, totalCount);
    }

    public async Task<IReadOnlyList<DsaProblemTopicReadItem>>
        GetTopicsByProblemIdsAsync(
            IReadOnlyCollection<Guid> problemIds,
            CancellationToken cancellationToken)
    {
        if (problemIds.Count == 0)
        {
            return [];
        }

        var problems = await dbContext.DsaProblems
            .AsNoTracking()
            .Where(problem => problemIds.Contains(problem.Id))
            .Include(problem => problem.Topics)
            .ToListAsync(cancellationToken);
        return problems.SelectMany(problem => problem.Topics.Select(topic =>
                new DsaProblemTopicReadItem(problem.Id, topic.Name)))
            .OrderBy(item => item.Name)
            .ToList();
    }

    public void Add(DsaProblem problem) =>
        dbContext.DsaProblems.Add(problem);

    public Task SaveChangesAsync(CancellationToken cancellationToken) =>
        dbContext.SaveChangesAsync(cancellationToken);
}
