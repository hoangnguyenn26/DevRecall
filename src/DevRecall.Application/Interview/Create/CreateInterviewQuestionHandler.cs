using DevRecall.Application.Identity;
using DevRecall.Domain.Interview;

namespace DevRecall.Application.Interview.Create;

public sealed class CreateInterviewQuestionHandler(
    IInterviewQuestionRepository repository,
    ICurrentUser currentUser)
{
    public async Task<CreateInterviewQuestionResult> HandleAsync(
        CreateInterviewQuestionCommand command,
        CancellationToken cancellationToken)
    {
        InterviewQuestionInputValidator.Validate(
            command.Title, command.Question, command.Topic, command.Notes);
        var userId = InterviewHandlerSupport.GetCurrentUserId(currentUser);
        var difficulty = InterviewQuestionDifficultyParser.Parse(
            command.Difficulty);
        var question = InterviewQuestion.Create(
            Guid.NewGuid(), userId, command.Title, command.Question,
            command.Topic, difficulty, command.Notes, DateTimeOffset.UtcNow);

        repository.Add(question);
        await repository.SaveChangesAsync(cancellationToken);

        return new CreateInterviewQuestionResult(
            question.Id, question.Title, question.Question, question.Topic,
            question.Difficulty.ToString(), question.Notes,
            question.Status.ToString(), question.CreatedAtUtc,
            question.UpdatedAtUtc);
    }

}
