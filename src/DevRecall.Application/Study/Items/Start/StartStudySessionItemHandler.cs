using DevRecall.Application.Common.Exceptions;
using DevRecall.Application.Common.Time;
using DevRecall.Application.Identity;
using DevRecall.Domain.Study;

namespace DevRecall.Application.Study.Items.Start;

public sealed record StartStudySessionItemCommand(
    Guid StudySessionId, Guid StudySessionItemId);

public sealed class StartStudySessionItemHandler(
    IStudySessionRepository repository,
    ICurrentUser currentUser,
    IUtcClock utcClock)
{
    public async Task<StudySessionItemStateResult> HandleAsync(
        StartStudySessionItemCommand command,
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

        try
        {
            session.StartItem(
                command.StudySessionItemId, utcClock.UtcNow);
        }
        catch (KeyNotFoundException)
        {
            throw new NotFoundException(
                StudySessionErrors.ItemNotFound.Code,
                StudySessionErrors.ItemNotFound.Message);
        }
        catch (InvalidOperationException)
        {
            throw StudySessionSupport.ItemMutationConflict(session);
        }

        await repository.SaveChangesAsync(cancellationToken);
        return StudyResultMapper.MapState(session.Items.Single(
            item => item.Id == command.StudySessionItemId));
    }
}
