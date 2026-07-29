using DevRecall.Application.Common.Exceptions;
using DevRecall.Application.Common.Time;
using DevRecall.Application.Identity;
using DevRecall.Domain.Study;

namespace DevRecall.Application.Study.Items.Reorder;

public sealed record ReorderStudySessionItemsCommand(
    Guid StudySessionId, IReadOnlyList<Guid> OrderedItemIds,
    int ExpectedVersion);

public sealed record ReorderStudySessionItemsResult(
    int Version, IReadOnlyList<StudySessionItemPositionResult> Items);

public sealed class ReorderStudySessionItemsHandler(
    IStudySessionRepository repository,
    ICurrentUser currentUser,
    IUtcClock utcClock)
{
    public async Task<ReorderStudySessionItemsResult> HandleAsync(
        ReorderStudySessionItemsCommand command,
        CancellationToken cancellationToken)
    {
        if (command.OrderedItemIds is null)
        {
            throw new ValidationException(
                new Dictionary<string, string[]>
                {
                    ["orderedItemIds"] = ["Ordered item ids are required."]
                });
        }
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
            changed = session.ReorderItems(
                command.ExpectedVersion, command.OrderedItemIds,
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
        catch (ArgumentException exception)
        {
            throw new ValidationException(
                new Dictionary<string, string[]>
                {
                    ["orderedItemIds"] = [exception.Message]
                });
        }

        if (changed)
        {
            await StudySessionSupport.SaveWithConcurrencyMappingAsync(
                repository, cancellationToken);
        }

        var items = session.Items.OrderBy(item => item.Position)
            .Select(item => new StudySessionItemPositionResult(
                item.Id, item.Position))
            .ToList();
        return new ReorderStudySessionItemsResult(session.Version, items);
    }
}
