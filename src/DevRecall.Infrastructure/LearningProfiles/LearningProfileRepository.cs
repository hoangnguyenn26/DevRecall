using DevRecall.Application.Common.Exceptions;
using DevRecall.Application.LearningProfiles;
using DevRecall.Domain.LearningProfiles;
using DevRecall.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace DevRecall.Infrastructure.LearningProfiles;

internal sealed class LearningProfileRepository(DevRecallDbContext dbContext) : ILearningProfileRepository
{
    public Task<LearningProfile?> GetAsync(Guid userId, CancellationToken cancellationToken) =>
        dbContext.LearningProfiles.Include(item => item.Technologies).Include(item => item.Goals)
            .SingleOrDefaultAsync(item => item.UserId == userId, cancellationToken);

    public void Add(LearningProfile profile) => dbContext.LearningProfiles.Add(profile);

    public async Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        try { await dbContext.SaveChangesAsync(cancellationToken); }
        catch (DbUpdateConcurrencyException exception)
        {
            throw new ConcurrencyException("LEARNING_PROFILE_CONFLICT",
                "The learning profile changed. Reload the latest version before saving again.", exception);
        }
        catch (DbUpdateException exception) when (exception.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
        {
            throw new ConcurrencyException("LEARNING_PROFILE_CONFLICT",
                "The learning profile changed. Reload the latest version before saving again.", exception);
        }
    }
}
