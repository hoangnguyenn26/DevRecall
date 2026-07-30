using DevRecall.Application.StudyPlans.Convert;
using DevRecall.Domain.Study;
using DevRecall.Domain.StudyPlans;
using DevRecall.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace DevRecall.Infrastructure.StudyPlans;

internal sealed class StudyPlanConversionPersistence(
    DevRecallDbContext dbContext) : IStudyPlanConversionPersistence
{
    public Task<StudyPlan?> GetPlanForUpdateAsync(
        Guid userId, Guid studyPlanId, CancellationToken cancellationToken) =>
        dbContext.StudyPlans.Include(plan => plan.Items)
            .SingleOrDefaultAsync(
                plan => plan.UserId == userId && plan.Id == studyPlanId,
                cancellationToken);

    public Task<StudySession?> GetStudySessionAsync(
        Guid userId, Guid studySessionId,
        CancellationToken cancellationToken) =>
        dbContext.StudySessions.AsNoTracking().Include(session => session.Items)
            .SingleOrDefaultAsync(
                session => session.UserId == userId
                    && session.Id == studySessionId,
                cancellationToken);

    public void AddStudySession(StudySession studySession) =>
        dbContext.StudySessions.Add(studySession);

    public async Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException exception)
        {
            throw new StudyPlanConversionConflictException(
                "The study plan changed concurrently.", exception);
        }
        catch (DbUpdateException exception)
        {
            throw new StudyPlanConversionConflictException(
                "The study plan conversion could not be persisted.", exception);
        }
    }
}
