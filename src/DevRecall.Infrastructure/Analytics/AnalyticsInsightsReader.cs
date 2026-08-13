using DevRecall.Application.Analytics;
using DevRecall.Application.Analytics.Insights;
using DevRecall.Domain.Dsa.Attempts;
using DevRecall.Domain.Interview.Practice;
using DevRecall.Domain.LearningContent;
using DevRecall.Domain.Reviews;
using DevRecall.Domain.Study;
using DevRecall.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace DevRecall.Infrastructure.Analytics;

internal sealed class AnalyticsInsightsReader(DevRecallDbContext db) : IAnalyticsInsightsReader
{
    public async Task<AnalyticsOverviewAggregate> ReadOverviewAsync(Guid userId, AnalyticsDateRange range, CancellationToken ct)
    {
        var sessions = await db.StudySessions.AsNoTracking().Where(x => x.UserId == userId && x.Status == StudySessionStatus.Completed && x.CompletedAtUtc >= range.FromUtc && x.CompletedAtUtc < range.ToUtc)
            .GroupBy(_ => 1).Select(g => new { Minutes = g.Sum(x => x.ActualDurationMinutes ?? 0), Count = g.Count() }).SingleOrDefaultAsync(ct);
        var reviews = await (from h in db.ReviewHistories.AsNoTracking() join i in db.ReviewItems.AsNoTracking() on h.ReviewItemId equals i.Id where i.UserId == userId && h.ReviewedAtUtc >= range.FromUtc && h.ReviewedAtUtc < range.ToUtc select h.Id).CountAsync(ct);
        var interviews = await db.InterviewPracticeAttempts.AsNoTracking().CountAsync(x => x.UserId == userId && x.CompletedAtUtc >= range.FromUtc && x.CompletedAtUtc < range.ToUtc, ct);
        var dsa = await (from a in db.DsaAttempts.AsNoTracking() join p in db.DsaProblems.AsNoTracking() on a.DsaProblemId equals p.Id where p.UserId == userId && a.AttemptedAtUtc >= range.FromUtc && a.AttemptedAtUtc < range.ToUtc select a.Id).CountAsync(ct);
        var lessons = await db.LearningContentCompletionEvidence.AsNoTracking()
            .CountAsync(x => x.UserId == userId && x.CompletedAtUtc >= range.FromUtc && x.CompletedAtUtc < range.ToUtc, ct);
        var activity = await ReadActivityAsync(userId, range, ct);
        return new(sessions?.Minutes ?? 0,
            activity.Count(x => x.StudyMinutes > 0 || x.PracticeCount > 0 || x.LessonsCompleted > 0),
            sessions?.Count ?? 0, reviews, interviews, dsa, lessons);
    }

    public async Task<IReadOnlyList<AnalyticsActivityPoint>> ReadActivityAsync(Guid userId, AnalyticsDateRange range, CancellationToken ct)
    {
        var sessions = await db.StudySessions.AsNoTracking().Where(x => x.UserId == userId && x.Status == StudySessionStatus.Completed && x.CompletedAtUtc >= range.FromUtc && x.CompletedAtUtc < range.ToUtc)
            .GroupBy(x => x.CompletedAtUtc!.Value.Date).Select(g => new { Date = g.Key, Minutes = g.Sum(x => x.ActualDurationMinutes ?? 0) }).ToListAsync(ct);
        var reviews = await (from h in db.ReviewHistories.AsNoTracking() join i in db.ReviewItems.AsNoTracking() on h.ReviewItemId equals i.Id where i.UserId == userId && h.ReviewedAtUtc >= range.FromUtc && h.ReviewedAtUtc < range.ToUtc group h by h.ReviewedAtUtc.Date into g select new { Date = g.Key, Count = g.Count() }).ToListAsync(ct);
        var interviews = await db.InterviewPracticeAttempts.AsNoTracking().Where(x => x.UserId == userId && x.CompletedAtUtc >= range.FromUtc && x.CompletedAtUtc < range.ToUtc).GroupBy(x => x.CompletedAtUtc.Date).Select(g => new { Date = g.Key, Count = g.Count() }).ToListAsync(ct);
        var dsa = await (from a in db.DsaAttempts.AsNoTracking() join p in db.DsaProblems.AsNoTracking() on a.DsaProblemId equals p.Id where p.UserId == userId && a.AttemptedAtUtc >= range.FromUtc && a.AttemptedAtUtc < range.ToUtc group a by a.AttemptedAtUtc.Date into g select new { Date = g.Key, Count = g.Count() }).ToListAsync(ct);
        var lessons = await db.LearningContentCompletionEvidence.AsNoTracking()
            .Where(x => x.UserId == userId && x.CompletedAtUtc >= range.FromUtc && x.CompletedAtUtc < range.ToUtc)
            .GroupBy(x => x.CompletedAtUtc.Date)
            .Select(g => new { Date = g.Key, Count = g.Count() }).ToListAsync(ct);
        var start = DateOnly.FromDateTime(range.FromUtc.UtcDateTime); var end = DateOnly.FromDateTime(range.ToUtc.AddTicks(-1).UtcDateTime); var points = new List<AnalyticsActivityPoint>();
        for (var day = start; day <= end; day = day.AddDays(1)) { var date = day.ToDateTime(TimeOnly.MinValue); points.Add(new(day, sessions.SingleOrDefault(x => x.Date == date)?.Minutes ?? 0, (reviews.SingleOrDefault(x => x.Date == date)?.Count ?? 0) + (interviews.SingleOrDefault(x => x.Date == date)?.Count ?? 0) + (dsa.SingleOrDefault(x => x.Date == date)?.Count ?? 0), lessons.SingleOrDefault(x => x.Date == date)?.Count ?? 0)); }
        return points;
    }

    public async Task<IReadOnlyList<AnalyticsRecentActivity>> ReadRecentActivityAsync(Guid userId,
        AnalyticsDateRange range, int take, CancellationToken cancellationToken) =>
        await (from evidence in db.LearningContentCompletionEvidence.AsNoTracking()
               join content in db.LearningContents.AsNoTracking()
                   on evidence.LearningContentId equals content.Id into contents
               from content in contents.DefaultIfEmpty()
               where evidence.UserId == userId
                   && evidence.CompletedAtUtc >= range.FromUtc
                   && evidence.CompletedAtUtc < range.ToUtc
               orderby evidence.CompletedAtUtc descending, evidence.Id descending
               select new AnalyticsRecentActivity(
                   "LearningContentCompleted", evidence.TitleSnapshot, evidence.CompletedAtUtc,
                   content != null && content.Status == ContentStatus.Published ? content.Slug : null,
                   content != null && content.Status == ContentStatus.Published))
            .Take(take).ToListAsync(cancellationToken);

    public async Task<(RatingDistribution Review, RatingDistribution Interview, RatingDistribution Dsa)> ReadPerformanceAsync(Guid userId, AnalyticsDateRange range, CancellationToken ct)
    {
        var review = await (from h in db.ReviewHistories.AsNoTracking() join i in db.ReviewItems.AsNoTracking() on h.ReviewItemId equals i.Id where i.UserId == userId && h.ReviewedAtUtc >= range.FromUtc && h.ReviewedAtUtc < range.ToUtc group h by 1 into g select new RatingDistribution(g.Count(x => x.Evaluation == ReviewEvaluation.Again), g.Count(x => x.Evaluation == ReviewEvaluation.Hard), g.Count(x => x.Evaluation == ReviewEvaluation.Good), g.Count(x => x.Evaluation == ReviewEvaluation.Easy))).SingleOrDefaultAsync(ct) ?? new(0, 0, 0, 0);
        var interview = await db.InterviewPracticeAttempts.AsNoTracking().Where(x => x.UserId == userId && x.CompletedAtUtc >= range.FromUtc && x.CompletedAtUtc < range.ToUtc).GroupBy(_ => 1).Select(g => new RatingDistribution(g.Count(x => x.SelfRating == InterviewSelfRating.NeedsWork), g.Count(x => x.SelfRating == InterviewSelfRating.Fair), g.Count(x => x.SelfRating == InterviewSelfRating.Good), g.Count(x => x.SelfRating == InterviewSelfRating.Strong))).SingleOrDefaultAsync(ct) ?? new(0, 0, 0, 0);
        var dsa = await (from a in db.DsaAttempts.AsNoTracking() join p in db.DsaProblems.AsNoTracking() on a.DsaProblemId equals p.Id where p.UserId == userId && a.AttemptedAtUtc >= range.FromUtc && a.AttemptedAtUtc < range.ToUtc group a by 1 into g select new RatingDistribution(g.Count(x => x.Result == DsaAttemptResult.Solved), g.Count(x => x.Result == DsaAttemptResult.PartiallySolved), g.Count(x => x.Result == DsaAttemptResult.Failed), g.Count(x => x.Result == DsaAttemptResult.Skipped))).SingleOrDefaultAsync(ct) ?? new(0, 0, 0, 0);
        return (review, interview, dsa);
    }
}
