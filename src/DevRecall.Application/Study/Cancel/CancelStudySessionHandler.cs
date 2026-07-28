using DevRecall.Application.Common.Exceptions;
using DevRecall.Application.Common.Time;
using DevRecall.Application.Identity;
using DevRecall.Domain.Study;

namespace DevRecall.Application.Study.Cancel;

public sealed record CancelStudySessionCommand(
    Guid StudySessionId, int ExpectedVersion);
public sealed record CancelStudySessionResult(
    Guid Id, string Status, DateTimeOffset? StartedAtUtc,
    int Version, DateTimeOffset UpdatedAtUtc);

public sealed class CancelStudySessionHandler(
    IStudySessionRepository repository,
    ICurrentUser currentUser,
    IUtcClock utcClock)
{
    public async Task<CancelStudySessionResult> HandleAsync(
        CancelStudySessionCommand command,
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

        bool changed;
        try
        {
            changed = session.Cancel(
                command.ExpectedVersion, utcClock.UtcNow);
        }
        catch (StudySessionDomainException exception)
        {
            throw StudySessionSupport.MapDomainConflict(exception);
        }
        catch (InvalidOperationException)
        {
            throw StudySessionSupport.ConflictForSessionStatus(session.Status);
        }

        if (changed)
        {
            await StudySessionSupport.SaveWithConcurrencyMappingAsync(
                repository, cancellationToken);
        }

        return new CancelStudySessionResult(
            session.Id, session.Status.ToString(),
            session.StartedAtUtc, session.Version, session.UpdatedAtUtc);
    }
}
