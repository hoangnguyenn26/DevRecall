using DevRecall.Application.Common.Exceptions;
using DevRecall.Application.Common.Time;
using DevRecall.Application.Identity;
using DevRecall.Domain.Study;

namespace DevRecall.Application.Study.Start;

public sealed record StartStudySessionCommand(Guid StudySessionId);
public sealed record StartStudySessionResult(
    Guid Id, string Status, DateTimeOffset StartedAtUtc,
    DateTimeOffset UpdatedAtUtc);

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
        var session = await repository.GetByIdAndUserIdForUpdateAsync(
            command.StudySessionId, userId, cancellationToken);
        if (session is null)
        {
            throw new NotFoundException(
                StudySessionErrors.SessionNotFound.Code,
                StudySessionErrors.SessionNotFound.Message);
        }

        if (session.Status != StudySessionStatus.Planned)
        {
            throw StudySessionSupport.ConflictForSessionStatus(session.Status);
        }

        session.Start(utcClock.UtcNow);
        await repository.SaveChangesAsync(cancellationToken);
        return new StartStudySessionResult(
            session.Id, session.Status.ToString(),
            session.StartedAtUtc!.Value, session.UpdatedAtUtc);
    }
}
