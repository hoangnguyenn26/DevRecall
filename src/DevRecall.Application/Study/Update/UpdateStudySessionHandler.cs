using DevRecall.Application.Common.Exceptions;
using DevRecall.Application.Common.Time;
using DevRecall.Application.Identity;
using DevRecall.Domain.Study;

namespace DevRecall.Application.Study.Update;

public sealed record UpdateStudySessionCommand(
    Guid StudySessionId, string Title,
    int PlannedDurationMinutes, string? Notes, int ExpectedVersion);

public sealed class UpdateStudySessionHandler(
    IStudySessionRepository repository,
    ICurrentUser currentUser,
    IUtcClock utcClock)
{
    public async Task<StudySessionResult> HandleAsync(
        UpdateStudySessionCommand command,
        CancellationToken cancellationToken)
    {
        StudySessionSupport.ValidatePlan(
            command.Title, command.PlannedDurationMinutes, command.Notes);
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
            changed = session.UpdatePlan(
                command.ExpectedVersion, command.Title,
                command.PlannedDurationMinutes,
                command.Notes, utcClock.UtcNow);
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

        return StudyResultMapper.Map(session);
    }
}
