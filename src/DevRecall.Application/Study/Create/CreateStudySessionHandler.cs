using DevRecall.Application.Common.Time;
using DevRecall.Application.Identity;
using DevRecall.Domain.Study;

namespace DevRecall.Application.Study.Create;

public sealed record CreateStudySessionCommand(
    string Title, int PlannedDurationMinutes, string? Notes);

public sealed class CreateStudySessionHandler(
    IStudySessionRepository repository,
    ICurrentUser currentUser,
    IUtcClock utcClock)
{
    public async Task<StudySessionResult> HandleAsync(
        CreateStudySessionCommand command,
        CancellationToken cancellationToken)
    {
        StudySessionSupport.ValidatePlan(
            command.Title, command.PlannedDurationMinutes, command.Notes);
        var session = StudySession.Create(
            Guid.NewGuid(), StudySessionSupport.GetUserId(currentUser),
            command.Title, command.PlannedDurationMinutes, command.Notes,
            utcClock.UtcNow);
        repository.Add(session);
        await repository.SaveChangesAsync(cancellationToken);
        return StudyResultMapper.Map(session);
    }
}
