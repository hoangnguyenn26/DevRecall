using DevRecall.Application.Common.Pagination;
using DevRecall.Application.Interview.Practice;
using DevRecall.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace DevRecall.Infrastructure.Interview.Practice;

internal sealed class InterviewPracticeHistoryReader(DevRecallDbContext dbContext)
    : IInterviewPracticeHistoryReader
{
    public async Task<PagedReadResult<InterviewPracticeHistorySummary>> ReadPageAsync(
        Guid userId, Guid questionId, int skip, int take,
        CancellationToken cancellationToken)
    {
        var query = dbContext.InterviewPracticeAttempts.AsNoTracking()
            .Where(item => item.UserId == userId && item.QuestionId == questionId);
        var totalCount = await query.CountAsync(cancellationToken);
        var rows = await query.OrderByDescending(item => item.CompletedAtUtc)
            .ThenByDescending(item => item.Id).Skip(skip).Take(take)
            .Select(item => new
            {
                item.Id, item.SelfRating, item.DurationSeconds,
                FollowUpsAnswered = item.FollowUps.Count, item.CompletedAtUtc
            })
            .ToListAsync(cancellationToken);
        var items = rows.Select(item => new InterviewPracticeHistorySummary(
            item.Id, item.SelfRating.ToString(), item.DurationSeconds,
            item.FollowUpsAnswered, item.CompletedAtUtc)).ToList();
        return new PagedReadResult<InterviewPracticeHistorySummary>(items, totalCount);
    }

    public async Task<InterviewPracticeAttemptDetail?> ReadDetailAsync(
        Guid userId, Guid questionId, Guid attemptId,
        CancellationToken cancellationToken)
    {
        var attempt = await dbContext.InterviewPracticeAttempts.AsNoTracking()
            .Where(item => item.Id == attemptId && item.QuestionId == questionId && item.UserId == userId)
            .Select(item => new
            {
                item.Id, item.QuestionId, item.QuestionSnapshot,
                item.AnswerSnapshot, item.ReferenceAnswerSnapshot,
                item.SelfRating, item.StartedAtUtc,
                item.CompletedAtUtc, item.DurationSeconds
            }).SingleOrDefaultAsync(cancellationToken);
        if (attempt is null) return null;
        var followUps = await dbContext.InterviewPracticeFollowUpAttempts.AsNoTracking()
            .Where(item => item.InterviewPracticeAttemptId == attemptId)
            .OrderBy(item => item.Id)
            .Select(item => new InterviewPracticeFollowUpDetail(
                item.FollowUpId, item.QuestionSnapshot, item.AnswerSnapshot))
            .ToListAsync(cancellationToken);
        return new InterviewPracticeAttemptDetail(
            attempt.Id, attempt.QuestionId, attempt.QuestionSnapshot,
            attempt.AnswerSnapshot, attempt.ReferenceAnswerSnapshot,
            attempt.SelfRating.ToString(), followUps, attempt.StartedAtUtc,
            attempt.CompletedAtUtc, attempt.DurationSeconds);
    }
}
