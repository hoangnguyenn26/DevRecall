using DevRecall.Application.Common.Exceptions;
using DevRecall.Application.Common.Time;
using DevRecall.Application.Identity;
using DevRecall.Domain.Study;

namespace DevRecall.Application.Study.Items.Skip;

public sealed record SkipStudySessionItemCommand(
    Guid StudySessionId, Guid StudySessionItemId,
    string? Notes, int ExpectedVersion, Guid SubmissionId);

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
            session.SkipItem(
                command.ExpectedVersion, command.StudySessionItemId,
                command.SubmissionId, command.Notes, utcClock.UtcNow);
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

        if (session.Version != command.ExpectedVersion)
            await StudySessionSupport.SaveWithConcurrencyMappingAsync(
                repository, cancellationToken);
        return StudyResultMapper.MapState(
            session.Items.Single(
                item => item.Id == command.StudySessionItemId),
            session.Version);
    }
}
