using DevRecall.Application.Common.Exceptions;
using DevRecall.Application.Common.Time;
using DevRecall.Application.Identity;
using DevRecall.Domain.Study;

namespace DevRecall.Application.Study.Items.Complete;

public sealed record CompleteStudySessionItemCommand(
    Guid StudySessionId, Guid StudySessionItemId,
    string? Notes, int ExpectedVersion);

public sealed class CompleteStudySessionItemHandler(
    IStudySessionRepository repository,
    ICurrentUser currentUser,
    IUtcClock utcClock)
{
    public async Task<StudySessionItemStateResult> HandleAsync(
        CompleteStudySessionItemCommand command,
        CancellationToken cancellationToken)
    {
        StudySessionSupport.ValidateItemNotes(command.Notes);
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

        try
        {
            session.CompleteItem(
                command.ExpectedVersion, command.StudySessionItemId,
                command.Notes, utcClock.UtcNow);
        }
        catch (StudySessionDomainException exception)
        {
            throw StudySessionSupport.MapDomainConflict(exception);
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

        await StudySessionSupport.SaveWithConcurrencyMappingAsync(
            repository, cancellationToken);
        return StudyResultMapper.MapState(
            session.Items.Single(
                item => item.Id == command.StudySessionItemId),
            session.Version);
    }
}
