using DevRecall.Application.Common.Exceptions;
using DevRecall.Application.Common.Time;
using DevRecall.Application.Identity;
using DevRecall.Application.Study.Resources;
using DevRecall.Domain.Study;

namespace DevRecall.Application.Study.Items.Add;

public sealed record AddStudySessionItemCommand(
    Guid StudySessionId, string ResourceType,
    Guid ResourceId, string? Notes, int ExpectedVersion);

public sealed class AddStudySessionItemHandler(
    IStudySessionRepository repository,
    IStudyResourceResolver resourceResolver,
    ICurrentUser currentUser,
    IUtcClock utcClock)
{
    public async Task<StudySessionItemResult> HandleAsync(
        AddStudySessionItemCommand command,
        CancellationToken cancellationToken)
    {
        if (command.ResourceId == Guid.Empty)
        {
            throw new ValidationException(
                new Dictionary<string, string[]>
                {
                    ["resourceId"] = ["Resource id is required."]
                });
        }

        StudySessionSupport.ValidateItemNotes(command.Notes);
        StudySessionSupport.ValidateExpectedVersion(command.ExpectedVersion);
        var resourceType = StudyResourceTypeParser.Parse(command.ResourceType);
        var userId = StudySessionSupport.GetUserId(currentUser);
        var session = await repository.GetByIdAndUserIdForUpdateAsync(
            command.StudySessionId, userId, cancellationToken);
        if (session is null)
        {
            throw new NotFoundException(
                StudySessionErrors.SessionNotFound.Code,
                StudySessionErrors.SessionNotFound.Message);
        }

        if (session.Version != command.ExpectedVersion)
        {
            throw new ConflictException(
                StudySessionErrors.Conflict.Code,
                StudySessionErrors.Conflict.Message);
        }

        StudySessionSupport.EnsureCanEditPlan(session);
        var resource = await resourceResolver.ResolveAsync(
            userId, resourceType, command.ResourceId, cancellationToken);
        if (resource is null)
        {
            throw new NotFoundException(
                StudySessionErrors.ResourceNotFound.Code,
                StudySessionErrors.ResourceNotFound.Message);
        }

        if (resource.Availability == StudyResourceAvailability.Archived)
        {
            throw new ConflictException(
                StudySessionErrors.ResourceArchived.Code,
                StudySessionErrors.ResourceArchived.Message);
        }

        StudySessionItem item;
        try
        {
            item = session.AddItem(
                command.ExpectedVersion, Guid.NewGuid(),
                resourceType, resource.ResourceId,
                command.Notes, utcClock.UtcNow);
        }
        catch (InvalidOperationException)
        {
            throw new ConflictException(
                StudySessionErrors.ItemAlreadyExists.Code,
                StudySessionErrors.ItemAlreadyExists.Message);
        }

        try
        {
            await repository.SaveChangesAsync(cancellationToken);
        }
        catch (StudySessionPersistenceConflictException exception)
            when (exception.ConstraintName == "concurrency")
        {
            throw new ConflictException(
                StudySessionErrors.Conflict.Code,
                StudySessionErrors.Conflict.Message);
        }
        catch (StudySessionPersistenceConflictException exception)
            when (exception.ConstraintName ==
                "ux_study_session_items_session_resource")
        {
            throw new ConflictException(
                StudySessionErrors.ItemAlreadyExists.Code,
                StudySessionErrors.ItemAlreadyExists.Message);
        }

        return StudyResultMapper.Map(item, session.Version);
    }
}
