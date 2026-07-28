using DevRecall.Application.Common.Exceptions;
using DevRecall.Application.Common.Time;
using DevRecall.Application.Identity;
using DevRecall.Domain.Study;

namespace DevRecall.Application.Study.Items.Reorder;

public sealed record ReorderStudySessionItemsCommand(
    Guid StudySessionId, IReadOnlyList<Guid> OrderedItemIds);

public sealed class ReorderStudySessionItemsHandler(
    IStudySessionRepository repository,
    ICurrentUser currentUser,
    IUtcClock utcClock)
{
    public async Task<IReadOnlyList<StudySessionItemPositionResult>> HandleAsync(
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
        bool changed;
        try
        {
            changed = session.ReorderItems(
                command.OrderedItemIds, utcClock.UtcNow);
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
            await repository.SaveChangesAsync(cancellationToken);
        }

        return session.Items.OrderBy(item => item.Position)
            .Select(item => new StudySessionItemPositionResult(
                item.Id, item.Position))
            .ToList();
    }
}
