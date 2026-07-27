using DevRecall.Application.Common.Exceptions;
using DevRecall.Application.Identity;
using DevRecall.Application.Interview.Answers;
using DevRecall.Application.Interview.FollowUps;
using DevRecall.Domain.Interview;
using DevRecall.Domain.Interview.Answers;
using DevRecall.Domain.Interview.FollowUps;

namespace DevRecall.Application.Interview.GetDetail;

public sealed class GetInterviewQuestionDetailHandler(
    IInterviewQuestionRepository questionRepository,
    IInterviewAnswerVersionRepository answerRepository,
    IInterviewFollowUpQuestionRepository followUpRepository,
    ICurrentUser currentUser)
{
    public async Task<GetInterviewQuestionDetailResult> HandleAsync(
        GetInterviewQuestionDetailQuery query,
        CancellationToken cancellationToken)
    {
        var userId = InterviewHandlerSupport.GetCurrentUserId(currentUser);
        var question = await questionRepository.GetByIdAndUserIdAsync(
            query.Id, userId, cancellationToken);

        if (question is null)
        {
            throw new NotFoundException(
                InterviewQuestionErrors.NotFound.Code,
                InterviewQuestionErrors.NotFound.Message);
        }

        var answerVersions = await answerRepository.GetByQuestionIdAsync(
            question.Id, cancellationToken);
        var followUps = await followUpRepository.GetActiveByQuestionIdAsync(
            question.Id, trackChanges: false, cancellationToken);
        var currentPublished = answerVersions.FirstOrDefault(answer =>
            answer.Status == InterviewAnswerVersionStatus.Published);
        var latestDraft = answerVersions.FirstOrDefault(answer =>
            answer.Status == InterviewAnswerVersionStatus.Draft);

        return new GetInterviewQuestionDetailResult(
            question.Id, question.Title, question.Question, question.Topic,
            question.Difficulty.ToString(), question.Notes,
            question.Status.ToString(), question.CreatedAtUtc,
            question.UpdatedAtUtc, MapAnswerDetail(currentPublished),
            MapAnswerDetail(latestDraft),
            answerVersions.Select(MapHistoryItem).ToList(),
            followUps.Select(MapFollowUp).ToList());
    }

    private static InterviewAnswerDetailItem? MapAnswerDetail(
        InterviewAnswerVersion? answer) =>
        answer is null
            ? null
            : new InterviewAnswerDetailItem(
                answer.Id, answer.VersionNumber, answer.Content,
                answer.Status.ToString(), answer.CreatedAtUtc,
                answer.UpdatedAtUtc, answer.PublishedAtUtc);

    private static InterviewAnswerHistoryItem MapHistoryItem(
        InterviewAnswerVersion answer) =>
        new(
            answer.Id, answer.VersionNumber, answer.Status.ToString(),
            answer.CreatedAtUtc, answer.UpdatedAtUtc, answer.PublishedAtUtc);

    private static InterviewFollowUpDetailItem MapFollowUp(
        InterviewFollowUpQuestion followUp) =>
        new(
            followUp.Id, followUp.Prompt, followUp.SortOrder,
            followUp.CreatedAtUtc, followUp.UpdatedAtUtc);
}
