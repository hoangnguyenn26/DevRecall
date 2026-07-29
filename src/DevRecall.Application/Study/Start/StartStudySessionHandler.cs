using DevRecall.Application.Common.Exceptions;
using DevRecall.Application.Common.Time;
using DevRecall.Application.Identity;
using DevRecall.Domain.Study;

namespace DevRecall.Application.Study.Start;

public sealed record StartStudySessionCommand(
    Guid StudySessionId, int ExpectedVersion);
public sealed record StartStudySessionResult(
    Guid Id, string Status, DateTimeOffset StartedAtUtc,
    int Version, DateTimeOffset UpdatedAtUtc);

public sealed class StartStudySessionHandler(
    IStudySessionRepository repository,
    ICurrentUser currentUser,
    IUtcClock utcClock)
{
    public async Task<StartStudySessionResult> HandleAsync(
        StartStudySessionCommand command,
        CancellationToken cancellationToken)
    {
        var userId = StudySessionSupport.GetUserId(currentUser);
        StudySessionSupport.ValidateExpectedVersion(command.ExpectedVersion);
        var session = await repository.GetByIdAndUserIdForUpdateAsync(
            command.StudySessionId, userId, cancellationToken);
        if (session is null)
        {
            throw new NotFoundException(
                StudySessionErrors.SessionNotFound.Code,
                StudySessionErrors.SessionNotFound.Message);
        }

        try
        {
            session.Start(command.ExpectedVersion, utcClock.UtcNow);
        }
        catch (StudySessionDomainException exception)
        {
            throw StudySessionSupport.MapDomainConflict(exception);
        }
        catch (InvalidOperationException)
        {
            throw StudySessionSupport.ConflictForSessionStatus(session.Status);
        }

        await StudySessionSupport.SaveWithConcurrencyMappingAsync(
            repository, cancellationToken);
        return new StartStudySessionResult(
            session.Id, session.Status.ToString(),
            session.StartedAtUtc!.Value, session.Version,
            session.UpdatedAtUtc);
    }
}
