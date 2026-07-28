using DevRecall.Application.Study;
using DevRecall.Domain.Study;
using DevRecall.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

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

    public Task SaveChangesAsync(CancellationToken cancellationToken) =>
        dbContext.SaveChangesAsync(cancellationToken);
}
