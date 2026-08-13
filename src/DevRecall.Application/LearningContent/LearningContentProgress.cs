using DevRecall.Application.Common.Exceptions;
using DevRecall.Application.Common.Time;
using DevRecall.Application.Identity;
using DevRecall.Domain.LearningContent;
using LearningContentAggregate = DevRecall.Domain.LearningContent.LearningContent;

namespace DevRecall.Application.LearningContent;

public sealed record LearningContentProgressItem(string Status, DateTimeOffset? StartedAtUtc,
    DateTimeOffset? CompletedAtUtc, int? Version, Guid? CompletionEvidenceId = null);

public interface ILearningContentProgressRepository
{
    Task<LearningContentAggregate?> GetPublishedContentAsync(string slug, CancellationToken cancellationToken);
    Task<LearningContentProgress?> GetAsync(Guid userId, Guid contentId, CancellationToken cancellationToken);
    Task<LearningContentCompletionEvidence?> GetCompletionEvidenceAsync(Guid userId, Guid contentId,
        CancellationToken cancellationToken);
    void Add(LearningContentProgress progress);
    void Add(LearningContentCompletionEvidence evidence);
    Task SaveChangesAsync(CancellationToken cancellationToken);
}

public sealed class LearningContentProgressRaceException(string message, Exception innerException)
    : Exception(message, innerException);

public sealed class StartLearningContentHandler(ILearningContentProgressRepository repository,
    ICurrentUser currentUser, IUtcClock clock)
{
    public async Task<LearningContentProgressItem> HandleAsync(string slug, CancellationToken cancellationToken)
    {
        var userId = RequireUser(currentUser);
        var content = await repository.GetPublishedContentAsync(NormalizeSlug(slug), cancellationToken)
            ?? throw NotFound();
        var progress = await repository.GetAsync(userId, content.Id, cancellationToken);
        var evidence = await repository.GetCompletionEvidenceAsync(userId, content.Id, cancellationToken);
        EnsureConsistent(progress, evidence);
        if (progress is null)
        {
            progress = LearningContentProgress.Start(Guid.NewGuid(), userId, content.Id, clock.UtcNow);
            repository.Add(progress);
            try { await repository.SaveChangesAsync(cancellationToken); }
            catch (LearningContentProgressRaceException)
            {
                progress = await repository.GetAsync(userId, content.Id, cancellationToken);
                if (progress is null) throw;
                evidence = await repository.GetCompletionEvidenceAsync(userId, content.Id, cancellationToken);
                EnsureConsistent(progress, evidence);
            }
        }
        return Map(progress) with { CompletionEvidenceId = evidence?.Id };
    }

    internal static Guid RequireUser(ICurrentUser user) => user.IsAuthenticated && user.UserId is { } id
        ? id : throw new UnauthorizedException("IDENTITY_UNAUTHENTICATED", "Authentication is required.");
    internal static string NormalizeSlug(string slug) => string.IsNullOrWhiteSpace(slug) ? "" : slug.Trim().ToLowerInvariant();
    internal static NotFoundException NotFound() => new("LEARNING_CONTENT_NOT_FOUND", "Learning content was not found.");
    internal static void EnsureConsistent(LearningContentProgress? progress,
        LearningContentCompletionEvidence? evidence)
    {
        var isCompleted = progress?.Status == LearningProgressStatus.Completed;
        if (isCompleted != (evidence is not null))
            throw new ConflictException("LEARNING_CONTENT_COMPLETION_INCONSISTENT",
                "Lesson progress and completion history are inconsistent. Run the explicit Development repair command.");
    }
    internal static LearningContentProgressItem Map(LearningContentProgress value) => new(
        value.Status.ToString(), value.StartedAtUtc, value.CompletedAtUtc, value.Version);
}

public sealed class CompleteLearningContentHandler(ILearningContentProgressRepository repository,
    ICurrentUser currentUser, IUtcClock clock)
{
    public async Task<LearningContentProgressItem> HandleAsync(string slug, int? expectedVersion,
        CancellationToken cancellationToken)
    {
        var userId = StartLearningContentHandler.RequireUser(currentUser);
        var content = await repository.GetPublishedContentAsync(StartLearningContentHandler.NormalizeSlug(slug), cancellationToken)
            ?? throw StartLearningContentHandler.NotFound();
        var progress = await repository.GetAsync(userId, content.Id, cancellationToken);
        var existingEvidence = await repository.GetCompletionEvidenceAsync(
            userId, content.Id, cancellationToken);
        StartLearningContentHandler.EnsureConsistent(progress, existingEvidence);
        if (progress?.Status == LearningProgressStatus.Completed)
        {
            return StartLearningContentHandler.Map(progress) with
            {
                CompletionEvidenceId = existingEvidence!.Id
            };
        }
        var now = clock.UtcNow;
        if (progress is null)
        {
            if (expectedVersion is not null) throw Conflict();
            progress = LearningContentProgress.CompleteDirectly(Guid.NewGuid(), userId, content.Id, now);
            repository.Add(progress);
        }
        else
        {
            if (expectedVersion is null) throw Conflict();
            try { progress.Complete(expectedVersion.Value, now); }
            catch (InvalidOperationException) { throw Conflict(); }
        }
        var evidence = LearningContentCompletionEvidence.Create(Guid.NewGuid(), userId, content.Id, content.Title, now);
        repository.Add(evidence);
        try { await repository.SaveChangesAsync(cancellationToken); }
        catch (LearningContentProgressRaceException)
        {
            var canonicalProgress = await repository.GetAsync(userId, content.Id, cancellationToken);
            var canonicalEvidence = await repository.GetCompletionEvidenceAsync(userId, content.Id, cancellationToken);
            if (canonicalProgress?.Status != LearningProgressStatus.Completed || canonicalEvidence is null) throw;
            return StartLearningContentHandler.Map(canonicalProgress) with
            {
                CompletionEvidenceId = canonicalEvidence.Id
            };
        }
        catch (ConcurrencyException)
        {
            var canonicalProgress = await repository.GetAsync(userId, content.Id, cancellationToken);
            var canonicalEvidence = await repository.GetCompletionEvidenceAsync(userId, content.Id, cancellationToken);
            if (canonicalProgress?.Status != LearningProgressStatus.Completed || canonicalEvidence is null) throw;
            return StartLearningContentHandler.Map(canonicalProgress) with
            {
                CompletionEvidenceId = canonicalEvidence.Id
            };
        }
        return StartLearningContentHandler.Map(progress) with { CompletionEvidenceId = evidence.Id };
    }

    private static ConcurrencyException Conflict() => new("LEARNING_CONTENT_PROGRESS_CONFLICT",
        "The lesson progress changed since it was loaded. Reload the lesson and try again.");
}
