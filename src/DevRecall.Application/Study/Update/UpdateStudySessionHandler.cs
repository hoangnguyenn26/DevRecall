using DevRecall.Application.Common.Exceptions;
using DevRecall.Application.Common.Time;
using DevRecall.Application.Identity;
using DevRecall.Domain.Study;

namespace DevRecall.Application.Study.Update;

public sealed record UpdateStudySessionCommand(
    Guid StudySessionId, string Title,
    int PlannedDurationMinutes, string? Notes);

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
        var userId = StudySessionSupport.GetUserId(currentUser);
        var session = await repository.GetByIdAndUserIdForUpdateAsync(
            command.StudySessionId, userId, cancellationToken);
        if (session is null)
        {
            throw new NotFoundException(
                StudySessionErrors.SessionNotFound.Code,
                StudySessionErrors.SessionNotFound.Message);
        }

        StudySessionSupport.EnsureCanEditPlan(session);
        if (session.UpdatePlan(
            command.Title, command.PlannedDurationMinutes,
            command.Notes, utcClock.UtcNow))
        {
            await repository.SaveChangesAsync(cancellationToken);
        }

        return StudyResultMapper.Map(session);
    }
}
