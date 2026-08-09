using DevRecall.Application.Study;
using DevRecall.Domain.Study;
using DevRecall.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace DevRecall.Infrastructure.Study;

internal sealed class StudySessionRepository(DevRecallDbContext dbContext)
    : IStudySessionRepository
{
    public Task<StudySession?> GetByIdAndUserIdAsync(
        Guid id, Guid userId, CancellationToken cancellationToken) =>
        dbContext.StudySessions
            .AsNoTracking()
            .Include(session => session.Items)
            .SingleOrDefaultAsync(
                session => session.Id == id && session.UserId == userId,
                cancellationToken);

    public Task<StudySession?> GetByIdAndUserIdForUpdateAsync(
        Guid id, Guid userId, CancellationToken cancellationToken) =>
        dbContext.StudySessions
            .Include(session => session.Items)
            .SingleOrDefaultAsync(
                session => session.Id == id && session.UserId == userId,
                cancellationToken);

    public void Add(StudySession session) =>
        dbContext.StudySessions.Add(session);

    public async Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException exception)
        {
            throw new StudySessionPersistenceConflictException(
                "concurrency", exception);
        }
        catch (DbUpdateException exception)
            when (exception.InnerException is PostgresException postgres
                && postgres.ConstraintName is not null)
        {
            throw new StudySessionPersistenceConflictException(
                postgres.ConstraintName, exception);
        }
    }
}
