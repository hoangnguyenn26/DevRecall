using DevRecall.Application.Dsa.Attempts;
using DevRecall.Domain.Dsa.Attempts;
using DevRecall.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace DevRecall.Infrastructure.Dsa.Attempts;

internal sealed class DsaAttemptRepository(DevRecallDbContext dbContext)
    : IDsaAttemptRepository
{
    public Task<DsaAttempt?> GetByIdAndProblemIdAsync(
        Guid id, Guid dsaProblemId, CancellationToken cancellationToken) =>
        dbContext.DsaAttempts
            .AsNoTracking()
            .SingleOrDefaultAsync(
                attempt => attempt.Id == id
                    && attempt.DsaProblemId == dsaProblemId,
                cancellationToken);

    public async Task<int> GetNextAttemptNumberAsync(
        Guid dsaProblemId, CancellationToken cancellationToken)
    {
        var maximumAttemptNumber = await dbContext.DsaAttempts
            .AsNoTracking()
            .Where(attempt => attempt.DsaProblemId == dsaProblemId)
            .Select(attempt => (int?)attempt.AttemptNumber)
            .MaxAsync(cancellationToken);
        return maximumAttemptNumber is null
            ? 1
            : maximumAttemptNumber.Value + 1;
    }

    public void Add(DsaAttempt attempt) =>
        dbContext.DsaAttempts.Add(attempt);

    public Task SaveChangesAsync(CancellationToken cancellationToken) =>
        dbContext.SaveChangesAsync(cancellationToken);
}
