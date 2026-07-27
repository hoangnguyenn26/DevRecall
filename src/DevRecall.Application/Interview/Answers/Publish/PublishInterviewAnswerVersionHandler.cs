using DevRecall.Application.Common.Exceptions;
using DevRecall.Application.Identity;
using DevRecall.Domain.Interview;
using DevRecall.Domain.Interview.Answers;

namespace DevRecall.Application.Interview.Answers.Publish;

public sealed class PublishInterviewAnswerVersionHandler(
    IInterviewQuestionRepository questionRepository,
    IInterviewAnswerVersionRepository answerRepository,
    ICurrentUser currentUser)
{
    public async Task<PublishInterviewAnswerVersionResult> HandleAsync(
        PublishInterviewAnswerVersionCommand command,
        CancellationToken cancellationToken)
    {
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
                "An answer of an archived interview question cannot be published.");
        }

        var answer = await answerRepository.GetByIdAndQuestionIdAsync(
            command.AnswerVersionId, question.Id, cancellationToken);

        if (answer is null)
        {
            throw new NotFoundException(
                InterviewAnswerVersionErrors.NotFound.Code,
                InterviewAnswerVersionErrors.NotFound.Message);
        }

        if (answer.Publish(DateTimeOffset.UtcNow))
        {
            await answerRepository.SaveChangesAsync(cancellationToken);
        }

        return new PublishInterviewAnswerVersionResult(
            answer.Id, answer.InterviewQuestionId, answer.VersionNumber,
            answer.Content, answer.Status.ToString(), answer.CreatedAtUtc,
            answer.UpdatedAtUtc, answer.PublishedAtUtc);
    }
}
