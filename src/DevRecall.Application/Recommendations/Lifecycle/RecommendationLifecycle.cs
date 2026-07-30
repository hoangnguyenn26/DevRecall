using DevRecall.Application.Common.Exceptions;
using DevRecall.Application.Common.Time;
using DevRecall.Application.Identity;
using DevRecall.Domain.Recommendations;

namespace DevRecall.Application.Recommendations.Lifecycle;

public sealed record DismissRecommendationCommand(
    Guid RecommendationId, int ExpectedVersion);
public sealed record CompleteRecommendationCommand(
    Guid RecommendationId, int ExpectedVersion);
public sealed record RecommendationMutationResult(
    Guid RecommendationId, string Status, DateTimeOffset UpdatedAtUtc,
    DateTimeOffset? DismissedAtUtc, DateTimeOffset? CompletedAtUtc,
    DateTimeOffset? ExpiredAtUtc, int Version);

public sealed class DismissRecommendationHandler(
    IStudyRecommendationRepository repository,
    ICurrentUser currentUser, IUtcClock utcClock)
{
    public Task<RecommendationMutationResult> HandleAsync(
        DismissRecommendationCommand command,
        CancellationToken cancellationToken) =>
        RecommendationLifecycleSupport.HandleAsync(
            command.RecommendationId, command.ExpectedVersion,
            static (item, version, now) => item.Dismiss(version, now),
            repository, currentUser, utcClock, cancellationToken);
}

public sealed class CompleteRecommendationHandler(
    IStudyRecommendationRepository repository,
    ICurrentUser currentUser, IUtcClock utcClock)
{
    public Task<RecommendationMutationResult> HandleAsync(
        CompleteRecommendationCommand command,
        CancellationToken cancellationToken) =>
        RecommendationLifecycleSupport.HandleAsync(
            command.RecommendationId, command.ExpectedVersion,
            static (item, version, now) => item.Complete(version, now),
            repository, currentUser, utcClock, cancellationToken);
}

internal static class RecommendationLifecycleSupport
{
    public static async Task<RecommendationMutationResult> HandleAsync(
        Guid recommendationId, int expectedVersion,
        Func<StudyRecommendation, int, DateTimeOffset, bool> transition,
        IStudyRecommendationRepository repository, ICurrentUser currentUser,
        IUtcClock utcClock, CancellationToken cancellationToken)
    {
        var errors = new Dictionary<string, string[]>();
        if (recommendationId == Guid.Empty)
        {
            errors["recommendationId"] = ["Recommendation id is required."];
        }

        if (expectedVersion <= 0)
        {
            errors["expectedVersion"] = ["Expected version must be greater than zero."];
        }

        if (errors.Count > 0)
        {
            throw new ValidationException(errors);
        }

        var userId = currentUser.UserId
            ?? throw new UnauthorizedException("AUTH_REQUIRED", "Authentication is required.");
        var item = await repository.GetByIdAndUserIdForUpdateAsync(
            recommendationId, userId, cancellationToken);
        if (item is null)
        {
            throw new NotFoundException(
                RecommendationErrors.NotFound.Code,
                RecommendationErrors.NotFound.Message);
        }

        bool changed;
        try
        {
            changed = transition(item, expectedVersion, utcClock.UtcNow);
        }
        catch (RecommendationDomainException exception)
        {
            throw new ConflictException(
                exception.Error.Code, exception.Error.Message);
        }

        if (changed)
        {
            try
            {
                await repository.SaveChangesAsync(cancellationToken);
            }
            catch (RecommendationPersistenceConflictException)
            {
                throw new ConflictException(
                    RecommendationErrors.Conflict.Code,
                    RecommendationErrors.Conflict.Message);
            }
        }

        return new(
            item.Id, item.Status.ToString(), item.UpdatedAtUtc,
            item.DismissedAtUtc, item.CompletedAtUtc, item.ExpiredAtUtc,
            item.Version);
    }
}
