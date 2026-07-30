using DevRecall.Application.StudyPlans;
using DevRecall.Domain.StudyPlans;
using DevRecall.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace DevRecall.Infrastructure.StudyPlans;

internal sealed class StudyPlanRepository(DevRecallDbContext dbContext)
    : IStudyPlanRepository
{
    public Task<StudyPlan?> GetByIdAndUserIdForUpdateAsync(
        Guid studyPlanId, Guid userId, CancellationToken cancellationToken) =>
        dbContext.StudyPlans.Include(plan => plan.Items)
            .SingleOrDefaultAsync(
                plan => plan.Id == studyPlanId && plan.UserId == userId,
                cancellationToken);

    public Task<StudyPlan?> GetDraftByUserIdForUpdateAsync(
        Guid userId, CancellationToken cancellationToken) =>
        dbContext.StudyPlans.Include(plan => plan.Items)
            .SingleOrDefaultAsync(
                plan => plan.UserId == userId
                    && plan.Status == StudyPlanStatus.Draft,
                cancellationToken);

    public void Add(StudyPlan studyPlan) => dbContext.StudyPlans.Add(studyPlan);

    public async Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException exception)
        {
            throw new StudyPlanPersistenceConflictException(
                "The study plan changed concurrently.", exception);
        }
        catch (DbUpdateException exception)
            when (IsConstraint(exception, "ux_study_plans_user_draft"))
        {
            throw new DraftStudyPlanAlreadyExistsException(
                "A draft study plan already exists.", exception);
        }
        catch (DbUpdateException exception)
            when (IsConstraint(exception, "ux_study_plan_items_plan_resource"))
        {
            throw new DuplicateStudyPlanResourceException(
                "The plan already contains the resource.", exception);
        }
    }

    private static bool IsConstraint(
        DbUpdateException exception, string constraintName) =>
        exception.InnerException is PostgresException
        {
            ConstraintName: var actual
        } && actual == constraintName;
}
