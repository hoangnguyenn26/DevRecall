using DevRecall.Application.Analytics;
using DevRecall.Application.Analytics.Overview;
using DevRecall.Domain.Study;
using DevRecall.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace DevRecall.Infrastructure.Analytics;

internal sealed class ProgressOverviewReader(DevRecallDbContext dbContext)
    : IProgressOverviewReader
{
    public async Task<ProgressOverviewReadModel> ReadAsync(
        Guid userId, AnalyticsDateRange range,
        CancellationToken cancellationToken)
    {
        var sessions = await ReadSessionsAsync(
            userId, range, cancellationToken);
        var items = await ReadItemsAsync(
            userId, range, cancellationToken);
        var reviews = await (
            from history in dbContext.ReviewHistories.AsNoTracking()
            join item in dbContext.ReviewItems.AsNoTracking()
                on history.ReviewItemId equals item.Id
            where item.UserId == userId
                && history.ReviewedAtUtc >= range.FromUtc
                && history.ReviewedAtUtc < range.ToUtc
            select history.Id).CountAsync(cancellationToken);
        var attempts = await (
            from attempt in dbContext.DsaAttempts.AsNoTracking()
            join problem in dbContext.DsaProblems.AsNoTracking()
                on attempt.DsaProblemId equals problem.Id
            where problem.UserId == userId
                && attempt.AttemptedAtUtc >= range.FromUtc
                && attempt.AttemptedAtUtc < range.ToUtc
            select attempt.Id).CountAsync(cancellationToken);
        var activeDays = await ReadActiveDaysAsync(
            userId, range, cancellationToken);
        return new ProgressOverviewReadModel(
            sessions.StudyMinutes, sessions.Completed,
            sessions.Cancelled, items.Completed, items.Skipped,
            reviews, attempts, items.InterviewCompleted,
            items.KnowledgeCompleted, activeDays);
    }

    private async Task<SessionAggregate> ReadSessionsAsync(
        Guid userId, AnalyticsDateRange range,
        CancellationToken cancellationToken)
    {
        var row = await dbContext.StudySessions.AsNoTracking()
            .Where(session => session.UserId == userId)
            .GroupBy(_ => 1)
            .Select(group => new SessionAggregate(
                group.Where(session =>
                    session.Status == StudySessionStatus.Completed
                    && session.CompletedAtUtc >= range.FromUtc
                    && session.CompletedAtUtc < range.ToUtc)
                    .Sum(session => session.ActualDurationMinutes ?? 0),
                group.Count(session =>
                    session.Status == StudySessionStatus.Completed
                    && session.CompletedAtUtc >= range.FromUtc
                    && session.CompletedAtUtc < range.ToUtc),
                // StudySession has no CancelledAtUtc; cancellation updates UpdatedAtUtc.
                group.Count(session =>
                    session.Status == StudySessionStatus.Cancelled
                    && session.UpdatedAtUtc >= range.FromUtc
                    && session.UpdatedAtUtc < range.ToUtc)))
            .SingleOrDefaultAsync(cancellationToken);
        return row ?? new SessionAggregate(0, 0, 0);
    }

    private async Task<ItemAggregate> ReadItemsAsync(
        Guid userId, AnalyticsDateRange range,
        CancellationToken cancellationToken)
    {
        var query =
            from item in dbContext.StudySessionItems.AsNoTracking()
            join session in dbContext.StudySessions.AsNoTracking()
                on item.StudySessionId equals session.Id
            where session.UserId == userId
                && item.CompletedAtUtc >= range.FromUtc
                && item.CompletedAtUtc < range.ToUtc
            select item;
        var row = await query.GroupBy(_ => 1)
            .Select(group => new ItemAggregate(
                group.Count(item =>
                    item.Status == StudySessionItemStatus.Completed),
                group.Count(item =>
                    item.Status == StudySessionItemStatus.Skipped),
                group.Count(item =>
                    item.Status == StudySessionItemStatus.Completed
                    && item.ResourceType
                        == StudyResourceType.InterviewQuestion),
                group.Count(item =>
                    item.Status == StudySessionItemStatus.Completed
                    && item.ResourceType
                        == StudyResourceType.KnowledgeNode)))
            .SingleOrDefaultAsync(cancellationToken);
        return row ?? new ItemAggregate(0, 0, 0, 0);
    }

    private Task<int> ReadActiveDaysAsync(
        Guid userId, AnalyticsDateRange range,
        CancellationToken cancellationToken) =>
        // Combine distinct UTC dates from every supported activity source.
        dbContext.Database.SqlQuery<int>(
            $"""
            SELECT COUNT(DISTINCT activity_date)::integer AS "Value"
            FROM (
                SELECT (completed_at_utc AT TIME ZONE 'UTC')::date AS activity_date
                FROM study_sessions
                WHERE user_id = {userId}
                  AND status = {(int)StudySessionStatus.Completed}
                  AND completed_at_utc >= {range.FromUtc}
                  AND completed_at_utc < {range.ToUtc}
                UNION
                SELECT (i.completed_at_utc AT TIME ZONE 'UTC')::date
                FROM study_session_items i
                JOIN study_sessions s ON s.id = i.study_session_id
                WHERE s.user_id = {userId}
                  AND i.status IN (
                    {(int)StudySessionItemStatus.Completed},
                    {(int)StudySessionItemStatus.Skipped})
                  AND i.completed_at_utc >= {range.FromUtc}
                  AND i.completed_at_utc < {range.ToUtc}
                UNION
                SELECT (h.reviewed_at_utc AT TIME ZONE 'UTC')::date
                FROM review_histories h
                JOIN review_items r ON r.id = h.review_item_id
                WHERE r.user_id = {userId}
                  AND h.reviewed_at_utc >= {range.FromUtc}
                  AND h.reviewed_at_utc < {range.ToUtc}
                UNION
                SELECT (a.attempted_at_utc AT TIME ZONE 'UTC')::date
                FROM dsa_attempts a
                JOIN dsa_problems p ON p.id = a.dsa_problem_id
                WHERE p.user_id = {userId}
                  AND a.attempted_at_utc >= {range.FromUtc}
                  AND a.attempted_at_utc < {range.ToUtc}
            ) activity
            """).SingleAsync(cancellationToken);

    private sealed record SessionAggregate(
        int StudyMinutes, int Completed, int Cancelled);
    private sealed record ItemAggregate(
        int Completed, int Skipped,
        int InterviewCompleted, int KnowledgeCompleted);
}
