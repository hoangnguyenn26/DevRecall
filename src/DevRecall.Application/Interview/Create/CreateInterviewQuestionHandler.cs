using DevRecall.Application.Common.Exceptions;
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
        Validate(command);
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

    private static void Validate(CreateInterviewQuestionCommand command)
    {
        var errors = new Dictionary<string, string[]>();
        ValidateRequired(
            command.Title, "title",
            InterviewQuestionText.TitleMaxLength, errors);
        ValidateRequired(
            command.Question, "question",
            InterviewQuestionText.QuestionMaxLength, errors);
        ValidateRequired(
            command.Topic, "topic",
            InterviewQuestionText.TopicMaxLength, errors);

        if (command.Notes?.Trim().Length
            > InterviewQuestionText.NotesMaxLength)
        {
            errors["notes"] =
                [$"Notes cannot exceed {InterviewQuestionText.NotesMaxLength} characters."];
        }

        if (errors.Count > 0)
        {
            throw new ValidationException(errors);
        }
    }

    private static void ValidateRequired(
        string? value, string fieldName, int maxLength,
        Dictionary<string, string[]> errors)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            errors[fieldName] =
                [$"{char.ToUpperInvariant(fieldName[0])}{fieldName[1..]} is required."];
            return;
        }

        var normalized = string.Join(
            ' ',
            value.Split(
                (char[]?)null,
                StringSplitOptions.RemoveEmptyEntries));

        if (normalized.Length > maxLength)
        {
            errors[fieldName] =
                [$"{char.ToUpperInvariant(fieldName[0])}{fieldName[1..]} cannot exceed {maxLength} characters."];
        }
    }
}
