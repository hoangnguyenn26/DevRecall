using DevRecall.Application.Common.Exceptions;
using DevRecall.Application.Identity;
using DevRecall.Domain.Interview;

namespace DevRecall.Application.Interview.GetDetail;

public sealed class GetInterviewQuestionDetailHandler(
    IInterviewQuestionRepository repository,
    ICurrentUser currentUser)
{
    public async Task<GetInterviewQuestionDetailResult> HandleAsync(
        GetInterviewQuestionDetailQuery query,
        CancellationToken cancellationToken)
    {
        var userId = InterviewHandlerSupport.GetCurrentUserId(currentUser);
        var question = await repository.GetByIdAndUserIdAsync(
            query.Id, userId, cancellationToken);

        if (question is null)
        {
            throw new NotFoundException(
                InterviewQuestionErrors.NotFound.Code,
                InterviewQuestionErrors.NotFound.Message);
        }

        return new GetInterviewQuestionDetailResult(
            question.Id, question.Title, question.Question, question.Topic,
            question.Difficulty.ToString(), question.Notes,
            question.Status.ToString(), question.CreatedAtUtc,
            question.UpdatedAtUtc);
    }
}
