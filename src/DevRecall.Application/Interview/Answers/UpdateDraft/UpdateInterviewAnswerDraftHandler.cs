using DevRecall.Application.Common.Exceptions;
using DevRecall.Application.Identity;
using DevRecall.Domain.Interview;
using DevRecall.Domain.Interview.Answers;

namespace DevRecall.Application.Interview.Answers.UpdateDraft;

public sealed class UpdateInterviewAnswerDraftHandler(
    IInterviewQuestionRepository questionRepository,
    IInterviewAnswerVersionRepository answerRepository,
    ICurrentUser currentUser)
{
    public async Task<UpdateInterviewAnswerDraftResult> HandleAsync(
        UpdateInterviewAnswerDraftCommand command,
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
                "An answer of an archived interview question cannot be modified.");
        }

        var answer = await answerRepository.GetByIdAndQuestionIdAsync(
            command.AnswerVersionId, question.Id, cancellationToken);

        if (answer is null)
        {
            throw new NotFoundException(
                InterviewAnswerVersionErrors.NotFound.Code,
                InterviewAnswerVersionErrors.NotFound.Message);
        }

        if (answer.Status == InterviewAnswerVersionStatus.Published)
        {
            throw new ConflictException(
                InterviewAnswerVersionErrors.Published.Code,
                InterviewAnswerVersionErrors.Published.Message);
        }

        if (answer.UpdateContent(command.Content, DateTimeOffset.UtcNow))
        {
            await answerRepository.SaveChangesAsync(cancellationToken);
        }

        return new UpdateInterviewAnswerDraftResult(
            answer.Id, answer.InterviewQuestionId, answer.VersionNumber,
            answer.Content, answer.Status.ToString(), answer.CreatedAtUtc,
            answer.UpdatedAtUtc, answer.PublishedAtUtc);
    }
}
