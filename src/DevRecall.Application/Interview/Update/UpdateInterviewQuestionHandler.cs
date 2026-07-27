using DevRecall.Application.Common.Exceptions;
using DevRecall.Application.Identity;
using DevRecall.Domain.Interview;

namespace DevRecall.Application.Interview.Update;

public sealed class UpdateInterviewQuestionHandler(
    IInterviewQuestionRepository repository,
    ICurrentUser currentUser)
{
    public async Task<UpdateInterviewQuestionResult> HandleAsync(
        UpdateInterviewQuestionCommand command,
        CancellationToken cancellationToken)
    {
        InterviewQuestionInputValidator.Validate(
            command.Title, command.Question, command.Topic, command.Notes);
        var difficulty = InterviewQuestionDifficultyParser.Parse(
            command.Difficulty);
        var userId = InterviewHandlerSupport.GetCurrentUserId(currentUser);
        var question = await repository.GetByIdAsync(
            command.Id, cancellationToken);

        if (question is null || question.UserId != userId)
        {
            throw new NotFoundException(
                InterviewQuestionErrors.NotFound.Code,
                InterviewQuestionErrors.NotFound.Message);
        }

        if (question.Status == InterviewQuestionStatus.Archived)
        {
            throw new ConflictException(
                InterviewQuestionErrors.Archived.Code,
                InterviewQuestionErrors.Archived.Message);
        }

        var changed = question.Update(
            command.Title, command.Question, command.Topic, difficulty,
            command.Notes, DateTimeOffset.UtcNow);

        if (changed)
        {
            await repository.SaveChangesAsync(cancellationToken);
        }

        return new UpdateInterviewQuestionResult(
            question.Id, question.Title, question.Question, question.Topic,
            question.Difficulty.ToString(), question.Notes,
            question.Status.ToString(), question.CreatedAtUtc,
            question.UpdatedAtUtc);
    }
}
