using DevRecall.Application.Common.Exceptions;
using DevRecall.Application.Common.Pagination;
using DevRecall.Application.Identity;
using DevRecall.Domain.Interview;

namespace DevRecall.Application.Interview.Practice;

public sealed record InterviewPracticeHistoryQuery(Guid QuestionId, int Page = 1, int PageSize = 10);
public sealed record InterviewPracticeHistorySummary(
    Guid AttemptId, string SelfRating, int DurationSeconds,
    int FollowUpsAnswered, DateTimeOffset CompletedAtUtc);
public sealed record InterviewPracticeHistoryResult(
    IReadOnlyList<InterviewPracticeHistorySummary> Items,
    int Page, int PageSize, int TotalCount, int TotalPages);
public sealed record InterviewPracticeFollowUpDetail(
    Guid FollowUpId, string QuestionSnapshot, string AnswerSnapshot);
public sealed record InterviewPracticeAttemptDetail(
    Guid AttemptId, Guid QuestionId, string QuestionSnapshot,
    string AnswerSnapshot, string? ReferenceAnswerSnapshot, string SelfRating,
    IReadOnlyList<InterviewPracticeFollowUpDetail> FollowUps,
    DateTimeOffset StartedAtUtc, DateTimeOffset CompletedAtUtc,
    int DurationSeconds);

public interface IInterviewPracticeHistoryReader
{
    Task<PagedReadResult<InterviewPracticeHistorySummary>> ReadPageAsync(
        Guid userId, Guid questionId, int skip, int take,
        CancellationToken cancellationToken);
    Task<InterviewPracticeAttemptDetail?> ReadDetailAsync(
        Guid userId, Guid questionId, Guid attemptId,
        CancellationToken cancellationToken);
}

public sealed class GetInterviewPracticeHistoryHandler(
    IInterviewQuestionRepository questionRepository,
    IInterviewPracticeHistoryReader reader,
    ICurrentUser currentUser)
{
    public async Task<InterviewPracticeHistoryResult> HandleAsync(
        InterviewPracticeHistoryQuery query, CancellationToken cancellationToken)
    {
        ValidatePagination(query.Page, query.PageSize);
        var userId = InterviewHandlerSupport.GetCurrentUserId(currentUser);
        var question = await questionRepository.GetByIdAndUserIdAsync(query.QuestionId, userId, cancellationToken);
        if (question is null)
            throw new NotFoundException(InterviewQuestionErrors.NotFound.Code, InterviewQuestionErrors.NotFound.Message);
        var page = await reader.ReadPageAsync(
            userId, query.QuestionId, (query.Page - 1) * query.PageSize,
            query.PageSize, cancellationToken);
        return new InterviewPracticeHistoryResult(
            page.Items, query.Page, query.PageSize, page.TotalCount,
            page.TotalCount == 0 ? 0 : (int)Math.Ceiling(page.TotalCount / (double)query.PageSize));
    }

    private static void ValidatePagination(int page, int pageSize)
    {
        var errors = new Dictionary<string, string[]>();
        if (page < 1) errors["page"] = ["Page must be greater than or equal to 1."];
        if (pageSize is < 1 or > 50) errors["pageSize"] = ["Page size must be between 1 and 50."];
        if (errors.Count > 0) throw new ValidationException(errors);
    }
}

public sealed class GetInterviewPracticeAttemptHandler(
    IInterviewPracticeHistoryReader reader, ICurrentUser currentUser)
{
    public async Task<InterviewPracticeAttemptDetail> HandleAsync(
        Guid questionId, Guid attemptId, CancellationToken cancellationToken)
    {
        var userId = InterviewHandlerSupport.GetCurrentUserId(currentUser);
        var attempt = await reader.ReadDetailAsync(userId, questionId, attemptId, cancellationToken);
        return attempt ?? throw new NotFoundException(
            "INTERVIEW_ATTEMPT_NOT_FOUND", "The interview practice attempt was not found.");
    }
}
