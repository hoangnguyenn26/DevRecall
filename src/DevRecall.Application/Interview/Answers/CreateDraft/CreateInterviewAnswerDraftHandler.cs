using DevRecall.Application.Common.Exceptions;
using DevRecall.Application.Identity;
using DevRecall.Domain.Interview;
using DevRecall.Domain.Interview.Answers;
using Microsoft.EntityFrameworkCore;

namespace DevRecall.Application.Interview.Answers.CreateDraft;

public sealed class CreateInterviewAnswerDraftHandler(
    IInterviewQuestionRepository questionRepository,
    IInterviewAnswerVersionRepository answerRepository,
    ICurrentUser currentUser)
{
    public async Task<CreateInterviewAnswerDraftResult> HandleAsync(
        CreateInterviewAnswerDraftCommand command,
        CancellationToken cancellationToken)
    {
        InterviewAnswerInputValidator.ValidateContent(command.Content);
        var userId = InterviewHandlerSupport.GetCurrentUserId(currentUser);
        var question = await questionRepository.GetByIdAsync(
            command.InterviewQuestionId, cancellationToken);

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
                "An archived interview question cannot receive a new answer draft.");
        }

        if (await answerRepository.HasDraftAsync(
            question.Id, cancellationToken))
        {
            throw new ConflictException(
                InterviewAnswerVersionErrors.DraftAlreadyExists.Code,
                InterviewAnswerVersionErrors.DraftAlreadyExists.Message);
        }

        var versionNumber = await answerRepository.GetNextVersionNumberAsync(
            question.Id, cancellationToken);
        var answer = InterviewAnswerVersion.CreateDraft(
            Guid.NewGuid(), question.Id, versionNumber,
            command.Content, DateTimeOffset.UtcNow);
        answerRepository.Add(answer);

        try
        {
            await answerRepository.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            throw new ConflictException(
                InterviewAnswerVersionErrors.VersionConflict.Code,
                InterviewAnswerVersionErrors.VersionConflict.Message);
        }

        return new CreateInterviewAnswerDraftResult(
            answer.Id, answer.InterviewQuestionId, answer.VersionNumber,
            answer.Content, answer.Status.ToString(), answer.CreatedAtUtc,
            answer.UpdatedAtUtc, answer.PublishedAtUtc);
    }
}
