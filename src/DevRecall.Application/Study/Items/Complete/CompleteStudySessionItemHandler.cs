using DevRecall.Application.Common.Exceptions;
using DevRecall.Application.Common.Time;
using DevRecall.Application.Identity;
using DevRecall.Domain.Study;

namespace DevRecall.Application.Study.Items.Complete;

public sealed record CompleteStudySessionItemCommand(
    Guid StudySessionId, Guid StudySessionItemId,
    string? Notes, int ExpectedVersion, Guid SubmissionId, Guid? EvidenceId);

public interface IStudySessionEvidenceValidator
{
    Task<bool> IsValidAsync(
        Guid userId, StudyResourceType resourceType, Guid resourceId,
        Guid? evidenceId, DateTimeOffset? sessionStartedAtUtc, CancellationToken cancellationToken);
}

public sealed class CompleteStudySessionItemHandler(
    IStudySessionRepository repository,
    ICurrentUser currentUser,
    IUtcClock utcClock,
    IStudySessionEvidenceValidator evidenceValidator)
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

        var item = session.Items.SingleOrDefault(
            item => item.Id == command.StudySessionItemId)
            ?? throw new NotFoundException(
                StudySessionErrors.ItemNotFound.Code,
                StudySessionErrors.ItemNotFound.Message);
        if (!await evidenceValidator.IsValidAsync(
            userId, item.ResourceType, item.ResourceId,
            command.EvidenceId, session.StartedAtUtc, cancellationToken))
        {
            throw new ValidationException(new Dictionary<string, string[]>
            {
                ["evidenceId"] = ["The completion evidence does not match this learning item."]
            });
        }

        try
        {
            session.CompleteItem(
                command.ExpectedVersion, command.StudySessionItemId,
                command.SubmissionId, command.EvidenceId,
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

        if (session.Version != command.ExpectedVersion)
            await StudySessionSupport.SaveWithConcurrencyMappingAsync(
                repository, cancellationToken);
        return StudyResultMapper.MapState(
            session.Items.Single(
                item => item.Id == command.StudySessionItemId),
            session.Version);
    }
}
