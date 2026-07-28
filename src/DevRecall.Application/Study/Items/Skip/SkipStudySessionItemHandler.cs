using DevRecall.Application.Common.Exceptions;
using DevRecall.Application.Common.Time;
using DevRecall.Application.Identity;
using DevRecall.Domain.Study;

namespace DevRecall.Application.Study.Items.Skip;

public sealed record SkipStudySessionItemCommand(
    Guid StudySessionId, Guid StudySessionItemId, string? Notes);

public sealed class SkipStudySessionItemHandler(
    IStudySessionRepository repository,
    ICurrentUser currentUser,
    IUtcClock utcClock)
{
    public async Task<StudySessionItemStateResult> HandleAsync(
        SkipStudySessionItemCommand command,
        CancellationToken cancellationToken)
    {
        StudySessionSupport.ValidateItemNotes(command.Notes);
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
            session.SkipItem(
                command.StudySessionItemId, command.Notes, utcClock.UtcNow);
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
