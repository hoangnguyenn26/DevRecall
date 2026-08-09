using DevRecall.Application.Common.Exceptions;
using DevRecall.Application.Common.Time;
using DevRecall.Application.Identity;
using DevRecall.Domain.Study;

namespace DevRecall.Application.Study.Reflection;

public sealed record UpdateStudySessionReflectionCommand(
    Guid StudySessionId, string? Reflection, int ExpectedVersion);
public sealed record UpdateStudySessionReflectionResult(
    Guid Id, string? Reflection, int Version, DateTimeOffset UpdatedAtUtc);

public sealed class UpdateStudySessionReflectionHandler(
    IStudySessionRepository repository, ICurrentUser currentUser,
    IUtcClock utcClock)
{
    public async Task<UpdateStudySessionReflectionResult> HandleAsync(
        UpdateStudySessionReflectionCommand command,
        CancellationToken cancellationToken)
    {
        StudySessionSupport.ValidateExpectedVersion(command.ExpectedVersion);
        if (command.Reflection?.Trim().Length > StudySessionText.ReflectionMaxLength)
        {
            throw new ValidationException(new Dictionary<string, string[]>
            {
                ["reflection"] =
                    [$"Reflection cannot exceed {StudySessionText.ReflectionMaxLength} characters."]
            });
        }

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
            changed = session.UpdateReflection(
                command.ExpectedVersion, command.Reflection, utcClock.UtcNow);
        }
        catch (StudySessionDomainException exception)
        {
            throw StudySessionSupport.MapDomainConflict(exception);
        }
        catch (InvalidOperationException)
        {
            throw new ConflictException(
                StudySessionErrors.SessionNotCompleted.Code,
                StudySessionErrors.SessionNotCompleted.Message);
        }

        if (changed)
        {
            await StudySessionSupport.SaveWithConcurrencyMappingAsync(
                repository, cancellationToken);
        }

        return new UpdateStudySessionReflectionResult(
            session.Id, session.Reflection, session.Version,
            session.UpdatedAtUtc);
    }
}
