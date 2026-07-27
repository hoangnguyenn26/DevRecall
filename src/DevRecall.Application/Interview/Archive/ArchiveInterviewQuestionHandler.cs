using DevRecall.Application.Common.Exceptions;
using DevRecall.Application.Identity;
using DevRecall.Domain.Interview;

namespace DevRecall.Application.Interview.Archive;

public sealed class ArchiveInterviewQuestionHandler(
    IInterviewQuestionRepository repository,
    ICurrentUser currentUser)
{
    public async Task HandleAsync(
        ArchiveInterviewQuestionCommand command,
        CancellationToken cancellationToken)
    {
        var userId = InterviewHandlerSupport.GetCurrentUserId(currentUser);
        var question = await repository.GetByIdAsync(
            command.Id, cancellationToken);

        if (question is null || question.UserId != userId)
        {
            throw new NotFoundException(
                InterviewQuestionErrors.NotFound.Code,
                InterviewQuestionErrors.NotFound.Message);
        }

        if (question.Archive(DateTimeOffset.UtcNow))
        {
            await repository.SaveChangesAsync(cancellationToken);
        }
    }
}
