using DevRecall.Application.Common.Exceptions;
using DevRecall.Application.Common.Time;
using DevRecall.Application.Identity;
using DevRecall.Domain.LearningContent;
using LearningContentAggregate = DevRecall.Domain.LearningContent.LearningContent;

namespace DevRecall.Application.LearningContent;

public sealed record LearningContentProgressItem(string Status, DateTimeOffset? StartedAtUtc,
    DateTimeOffset? CompletedAtUtc, int? Version);

public interface ILearningContentProgressRepository
{
    Task<LearningContentAggregate?> GetPublishedContentAsync(string slug, CancellationToken cancellationToken);
    Task<LearningContentProgress?> GetAsync(Guid userId, Guid contentId, CancellationToken cancellationToken);
    void Add(LearningContentProgress progress);
    void Add(LearningContentCompletionEvidence evidence);
    Task SaveChangesAsync(CancellationToken cancellationToken);
}

public sealed class StartLearningContentHandler(ILearningContentProgressRepository repository,
    ICurrentUser currentUser, IUtcClock clock)
{
    public async Task<LearningContentProgressItem> HandleAsync(string slug, CancellationToken cancellationToken)
    {
        var userId = RequireUser(currentUser);
        var content = await repository.GetPublishedContentAsync(NormalizeSlug(slug), cancellationToken)
            ?? throw NotFound();
        var progress = await repository.GetAsync(userId, content.Id, cancellationToken);
        if (progress is null)
        {
            progress = LearningContentProgress.Start(Guid.NewGuid(), userId, content.Id, clock.UtcNow);
            repository.Add(progress);
            await repository.SaveChangesAsync(cancellationToken);
        }
        return Map(progress);
    }

    internal static Guid RequireUser(ICurrentUser user) => user.IsAuthenticated && user.UserId is { } id
        ? id : throw new UnauthorizedException("IDENTITY_UNAUTHENTICATED", "Authentication is required.");
    internal static string NormalizeSlug(string slug) => string.IsNullOrWhiteSpace(slug) ? "" : slug.Trim().ToLowerInvariant();
    internal static NotFoundException NotFound() => new("LEARNING_CONTENT_NOT_FOUND", "Learning content was not found.");
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
        if (progress?.Status == LearningProgressStatus.Completed) return StartLearningContentHandler.Map(progress);
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
        repository.Add(LearningContentCompletionEvidence.Create(Guid.NewGuid(), userId, content.Id, content.Title, now));
        await repository.SaveChangesAsync(cancellationToken);
        return StartLearningContentHandler.Map(progress);
    }

    private static ConcurrencyException Conflict() => new("LEARNING_CONTENT_PROGRESS_CONFLICT",
        "The lesson progress changed since it was loaded. Reload the lesson and try again.");
}
