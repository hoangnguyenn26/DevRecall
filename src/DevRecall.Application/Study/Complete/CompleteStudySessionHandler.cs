using DevRecall.Application.Common.Exceptions;
using DevRecall.Application.Common.Time;
using DevRecall.Application.Identity;
using DevRecall.Domain.Study;

namespace DevRecall.Application.Study.Complete;

public sealed record CompleteStudySessionCommand(
    Guid StudySessionId, int ExpectedVersion);

public sealed record StudySessionCompletionSummaryResult(
    int TotalItems, int PendingItems, int InProgressItems,
    int CompletedItems, int SkippedItems,
    int KnowledgeItemsCompleted, int InterviewItemsCompleted,
    int DsaItemsCompleted, int ReviewItemsCompleted);

public sealed record CompleteStudySessionResult(
    Guid Id, string Status, DateTimeOffset StartedAtUtc,
    DateTimeOffset CompletedAtUtc, int ActualDurationMinutes,
    int PlannedDurationMinutes, int Version,
    StudySessionCompletionSummaryResult Summary,
    DateTimeOffset UpdatedAtUtc);

public sealed class CompleteStudySessionHandler(
    IStudySessionRepository repository,
    ICurrentUser currentUser,
    IUtcClock utcClock)
{
    public async Task<CompleteStudySessionResult> HandleAsync(
        CompleteStudySessionCommand command,
        CancellationToken cancellationToken)
    {
        StudySessionSupport.ValidateExpectedVersion(command.ExpectedVersion);
        var userId = StudySessionSupport.GetUserId(currentUser);
        var session = await repository.GetByIdAndUserIdForUpdateAsync(
            command.StudySessionId, userId, cancellationToken);
        if (session is null)
        {
            throw new NotFoundException(
                StudySessionErrors.SessionNotFound.Code,
                StudySessionErrors.SessionNotFound.Message);
        }

        StudySessionCompletionSummary summary;
        try
        {
            summary = session.Complete(
                command.ExpectedVersion, utcClock.UtcNow);
        }
        catch (StudySessionDomainException exception)
        {
            throw StudySessionSupport.MapDomainConflict(exception);
        }
        catch (InvalidOperationException)
        {
            throw StudySessionSupport.ItemMutationConflict(session);
        }

        await StudySessionSupport.SaveWithConcurrencyMappingAsync(
            repository, cancellationToken);
        return new CompleteStudySessionResult(
            session.Id, session.Status.ToString(),
            session.StartedAtUtc!.Value, session.CompletedAtUtc!.Value,
            session.ActualDurationMinutes!.Value,
            session.PlannedDurationMinutes, session.Version,
            MapSummary(summary), session.UpdatedAtUtc);
    }

    private static StudySessionCompletionSummaryResult MapSummary(
        StudySessionCompletionSummary summary) =>
        new(
            summary.TotalItems, summary.PendingItems,
            summary.InProgressItems, summary.CompletedItems,
            summary.SkippedItems, summary.KnowledgeItemsCompleted,
            summary.InterviewItemsCompleted, summary.DsaItemsCompleted,
            summary.ReviewItemsCompleted);
}
