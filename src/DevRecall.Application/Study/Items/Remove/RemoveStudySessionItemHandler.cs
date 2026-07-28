using DevRecall.Application.Common.Exceptions;
using DevRecall.Application.Common.Time;
using DevRecall.Application.Identity;
using DevRecall.Domain.Study;

namespace DevRecall.Application.Study.Items.Remove;

public sealed record RemoveStudySessionItemCommand(
    Guid StudySessionId, Guid StudySessionItemId);

public sealed class RemoveStudySessionItemHandler(
    IStudySessionRepository repository,
    ICurrentUser currentUser,
    IUtcClock utcClock)
{
    public async Task HandleAsync(
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

        StudySessionSupport.EnsureCanEditPlan(session);
        if (!session.RemoveItem(
            command.StudySessionItemId, utcClock.UtcNow))
        {
            throw new NotFoundException(
                StudySessionErrors.ItemNotFound.Code,
                StudySessionErrors.ItemNotFound.Message);
        }

        await repository.SaveChangesAsync(cancellationToken);
    }
}
