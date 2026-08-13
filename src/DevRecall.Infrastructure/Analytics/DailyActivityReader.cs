using DevRecall.Application.Analytics;
using DevRecall.Application.Analytics.DailyActivity;
using DevRecall.Domain.Study;
using DevRecall.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace DevRecall.Infrastructure.Analytics;

internal sealed class DailyActivityReader(DevRecallDbContext dbContext)
    : IDailyActivityReader
{
    public async Task<IReadOnlyList<DailyActivityAggregate>> ReadAsync(
        Guid userId, AnalyticsDateRange range,
        CancellationToken cancellationToken)
    {
        var sessions = await dbContext.Database.SqlQuery<SessionDailyRow>(
            $"""
            SELECT
                (completed_at_utc AT TIME ZONE 'UTC')::date AS date,
                COALESCE(SUM(actual_duration_minutes), 0)::integer AS study_minutes,
                COUNT(*)::integer AS completed_sessions
            FROM study_sessions
            WHERE user_id = {userId}
              AND status = {(int)StudySessionStatus.Completed}
              AND completed_at_utc >= {range.FromUtc}
              AND completed_at_utc < {range.ToUtc}
            GROUP BY (completed_at_utc AT TIME ZONE 'UTC')::date
            """).ToListAsync(cancellationToken);
        var studyItems = await ReadCountsAsync(
            $"""
            SELECT
                (i.completed_at_utc AT TIME ZONE 'UTC')::date AS date,
                COUNT(*)::integer AS count
            FROM study_session_items i
            JOIN study_sessions s ON s.id = i.study_session_id
            WHERE s.user_id = {userId}
              AND i.status = {(int)StudySessionItemStatus.Completed}
              AND i.completed_at_utc >= {range.FromUtc}
              AND i.completed_at_utc < {range.ToUtc}
            GROUP BY (i.completed_at_utc AT TIME ZONE 'UTC')::date
            """,
            cancellationToken);
        var reviews = await ReadCountsAsync(
            $"""
            SELECT
                (h.reviewed_at_utc AT TIME ZONE 'UTC')::date AS date,
                COUNT(*)::integer AS count
            FROM review_histories h
            JOIN review_items i ON i.id = h.review_item_id
            WHERE i.user_id = {userId}
              AND h.reviewed_at_utc >= {range.FromUtc}
              AND h.reviewed_at_utc < {range.ToUtc}
            GROUP BY (h.reviewed_at_utc AT TIME ZONE 'UTC')::date
            """,
            cancellationToken);
        var attempts = await ReadCountsAsync(
            $"""
            SELECT
                (a.attempted_at_utc AT TIME ZONE 'UTC')::date AS date,
                COUNT(*)::integer AS count
            FROM dsa_attempts a
            JOIN dsa_problems p ON p.id = a.dsa_problem_id
            WHERE p.user_id = {userId}
              AND a.attempted_at_utc >= {range.FromUtc}
              AND a.attempted_at_utc < {range.ToUtc}
            GROUP BY (a.attempted_at_utc AT TIME ZONE 'UTC')::date
            """,
            cancellationToken);
        var lessons = await ReadCountsAsync(
            $"""
            SELECT
                (completed_at_utc AT TIME ZONE 'UTC')::date AS date,
                COUNT(*)::integer AS count
            FROM learning_content_completion_evidence
            WHERE user_id = {userId}
              AND completed_at_utc >= {range.FromUtc}
              AND completed_at_utc < {range.ToUtc}
            GROUP BY (completed_at_utc AT TIME ZONE 'UTC')::date
            """,
            cancellationToken);

        var dates = sessions.Select(row => row.Date)
            .Concat(studyItems.Select(row => row.Date))
            .Concat(reviews.Select(row => row.Date))
            .Concat(attempts.Select(row => row.Date))
            .Concat(lessons.Select(row => row.Date))
            .Distinct()
            .Order()
            .ToArray();
        var sessionsByDate = sessions.ToDictionary(row => row.Date);
        var itemsByDate = studyItems.ToDictionary(row => row.Date, row => row.Count);
        var reviewsByDate = reviews.ToDictionary(row => row.Date, row => row.Count);
        var attemptsByDate = attempts.ToDictionary(row => row.Date, row => row.Count);
        var lessonsByDate = lessons.ToDictionary(row => row.Date, row => row.Count);
        return dates.Select(date =>
        {
            sessionsByDate.TryGetValue(date, out var session);
            return new DailyActivityAggregate(
                date, session?.StudyMinutes ?? 0,
                session?.CompletedSessions ?? 0,
                itemsByDate.GetValueOrDefault(date),
                reviewsByDate.GetValueOrDefault(date),
                attemptsByDate.GetValueOrDefault(date),
                lessonsByDate.GetValueOrDefault(date));
        }).ToList();
    }

    private Task<List<DailyCountRow>> ReadCountsAsync(
        FormattableString sql, CancellationToken cancellationToken) =>
        dbContext.Database.SqlQuery<DailyCountRow>(sql)
            .ToListAsync(cancellationToken);

    private sealed class SessionDailyRow
    {
        public DateOnly Date { get; init; }
        public int StudyMinutes { get; init; }
        public int CompletedSessions { get; init; }
    }

    private sealed class DailyCountRow
    {
        public DateOnly Date { get; init; }
        public int Count { get; init; }
    }
}
