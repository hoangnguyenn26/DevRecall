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

    public void Add(DsaProblem problem) =>
        dbContext.DsaProblems.Add(problem);

    public Task SaveChangesAsync(CancellationToken cancellationToken) =>
        dbContext.SaveChangesAsync(cancellationToken);
}
