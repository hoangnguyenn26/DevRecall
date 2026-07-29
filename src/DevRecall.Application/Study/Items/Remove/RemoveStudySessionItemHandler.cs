using DevRecall.Application.Common.Exceptions;
using DevRecall.Application.Common.Time;
using DevRecall.Application.Identity;
using DevRecall.Domain.Study;

namespace DevRecall.Application.Study.Items.Remove;

public sealed record RemoveStudySessionItemCommand(
    Guid StudySessionId, Guid StudySessionItemId, int ExpectedVersion);

public sealed class RemoveStudySessionItemHandler(
    IStudySessionRepository repository,
    ICurrentUser currentUser,
    IUtcClock utcClock)
{
    public async Task<int> HandleAsync(
        RemoveStudySessionItemCommand command,
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

        StudySessionSupport.ValidateExpectedVersion(command.ExpectedVersion);
        bool removed;
        try
        {
            removed = session.RemoveItem(
                command.ExpectedVersion, command.StudySessionItemId,
                utcClock.UtcNow);
        }
        catch (StudySessionDomainException exception)
        {
            throw StudySessionSupport.MapDomainConflict(exception);
        }
        catch (InvalidOperationException)
        {
            throw StudySessionSupport.ConflictForSessionStatus(session.Status);
        }

        if (!removed)
        {
            throw new NotFoundException(
                StudySessionErrors.ItemNotFound.Code,
                StudySessionErrors.ItemNotFound.Message);
        }

        await StudySessionSupport.SaveWithConcurrencyMappingAsync(
            repository, cancellationToken);
        return session.Version;
    }
}
