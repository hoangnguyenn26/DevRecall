using DevRecall.Application.Recommendations;
using DevRecall.Domain.Recommendations;
using DevRecall.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace DevRecall.Infrastructure.Recommendations;

internal sealed class StudyRecommendationRepository(DevRecallDbContext dbContext)
    : IStudyRecommendationRepository
{
    public Task<StudyRecommendation?> GetActiveByUserAndResourceForUpdateAsync(
        Guid userId, RecommendationResourceType resourceType, Guid resourceId,
        RecommendationType type, CancellationToken cancellationToken) =>
        dbContext.StudyRecommendations.SingleOrDefaultAsync(
            x => x.UserId == userId && x.ResourceType == resourceType
                && x.ResourceId == resourceId && x.Type == type
                && x.Status == RecommendationStatus.Active,
            cancellationToken);

    public Task<StudyRecommendation?> GetByIdAndUserIdForUpdateAsync(
        Guid recommendationId, Guid userId, CancellationToken cancellationToken) =>
        dbContext.StudyRecommendations.SingleOrDefaultAsync(
            x => x.Id == recommendationId && x.UserId == userId,
            cancellationToken);

    public void Add(StudyRecommendation recommendation) =>
        dbContext.StudyRecommendations.Add(recommendation);

    public async Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException exception)
        {
            throw new RecommendationPersistenceConflictException(
                "The recommendation changed concurrently.", exception);
        }
        catch (DbUpdateException exception)
            when (exception.InnerException is PostgresException
            {
                ConstraintName:
                    "ux_study_recommendations_active_user_resource_type"
            })
        {
            throw new ActiveRecommendationAlreadyExistsException(
                "An active recommendation already exists.", exception);
        }
    }
}
