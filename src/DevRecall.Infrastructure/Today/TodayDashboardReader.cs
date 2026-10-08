using System.Globalization;
using DevRecall.Application.Analytics;
using DevRecall.Application.Analytics.DailyActivity;
using DevRecall.Application.Analytics.Overview;
using DevRecall.Application.Today;
using DevRecall.Domain.Dsa;
using DevRecall.Domain.Interview;
using DevRecall.Domain.Knowledge;
using DevRecall.Domain.Recommendations;
using DevRecall.Domain.Reviews;
using DevRecall.Domain.Study;
using DevRecall.Domain.StudyPlans;
using DevRecall.Domain.WeakTopics;
using DevRecall.Domain.LearningContent;
using DevRecall.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace DevRecall.Infrastructure.Today;

internal sealed class TodayDashboardReader(
    DevRecallDbContext dbContext,
    IProgressOverviewReader progressOverviewReader,
    IDailyActivityReader dailyActivityReader) : ITodayDashboardReader
{
    private const int RecommendationPreviewLimit = 3;

    public async Task<TodayDashboardReadModel> ReadAsync(
        Guid userId, DateTimeOffset currentUtc,
        CancellationToken cancellationToken)
    {
        var user = await dbContext.Users.AsNoTracking()
            .Where(user => user.Id == userId)
            .Select(user => new
            {
                user.DisplayName,
                WeeklyTargetDays = dbContext.UserLearningPreferences
                    .Where(preference => preference.UserId == user.Id)
                    .Select(preference => (int?)preference.WeeklyTargetDays)
                    .SingleOrDefault()
            })
            .SingleAsync(cancellationToken);
        var weeklyTargetDays = user.WeeklyTargetDays
            ?? TodayDashboardDefaults.WeeklyTargetDays;
        var week = GetCurrentWeek(currentUtc);
        var analyticsRange = new AnalyticsDateRange(week.StartUtc, week.EndUtc);
        var reviewsDue = await dbContext.ReviewItems.AsNoTracking()
            .CountAsync(item => item.UserId == userId
                && item.Status == ReviewItemStatus.Active
                && item.DueAtUtc <= currentUtc, cancellationToken);
        var progress = await progressOverviewReader.ReadAsync(
            userId, analyticsRange, cancellationToken);
        var dailyActivity = await dailyActivityReader.ReadAsync(
            userId, analyticsRange, cancellationToken);
        var activeSession = await ReadActiveSessionAsync(
            userId, cancellationToken);
        var plan = await ReadStudyPlanAsync(userId, cancellationToken);
        var recommendations = await ReadRecommendationsAsync(
            userId, currentUtc, cancellationToken);
        var activeRecommendationCount = await dbContext.StudyRecommendations
            .AsNoTracking()
            .CountAsync(item => item.UserId == userId
                && item.Status == RecommendationStatus.Active
                && (item.ExpiresAtUtc == null || item.ExpiresAtUtc > currentUtc),
                cancellationToken);
        var weakTopics = await ReadWeakTopicsAsync(
            userId, cancellationToken);
        var summaries = await ReadResourceSummariesAsync(
            userId, plan, recommendations, weakTopics, cancellationToken);
        var activityByDate = dailyActivity.ToDictionary(item => item.Date);
        var activity = Enumerable.Range(0, 7)
            .Select(offset =>
            {
                var date = week.StartDate.AddDays(offset);
                activityByDate.TryGetValue(date, out var point);
                return new TodayActivityPointReadModel(
                    date, point?.StudyMinutes ?? 0,
                    point is null ? 0 : point.CompletedSessions
                        + point.CompletedStudyItems + point.Reviews
                        + point.DsaAttempts);
            })
            .ToArray();
        var progressPercent = Math.Min(
            100m, progress.ActiveStudyDays
                / weeklyTargetDays * 100m);

        return new TodayDashboardReadModel(
            user.DisplayName, user.WeeklyTargetDays.HasValue,
            new(reviewsDue, progress.StudyMinutes, progress.ActiveStudyDays,
                weeklyTargetDays,
                decimal.Round(progressPercent, 2)),
            MapPlan(plan, summaries),
            recommendations.Take(3)
                .Select(item => new TodayRecommendationReadModel(
                    item.Id, item.ResourceType, item.ResourceId, item.Type,
                    item.Priority, item.PriorityScore,
                    GetSummary(summaries, item.ResourceType, item.ResourceId).Title,
                    RecommendationSummary(item),
                    GetSummary(summaries, item.ResourceType, item.ResourceId).Available))
                .ToArray(),
            weakTopics.Select(item =>
            {
                var summary = GetSummary(
                    summaries, item.ResourceType, item.ResourceId);
                return new TodayWeakTopicReadModel(
                    item.Id, item.ResourceType, item.ResourceId, item.Level,
                    item.Score, summary.Title, WeakTopicSummary(item),
                    summary.Available);
            }).ToArray(),
            activity, activeSession, activeRecommendationCount)
        {
            HasActionablePlan = plan?.HasActionableItems == true
        };
    }

    private Task<ActiveStudySessionCandidate?> ReadActiveSessionAsync(
        Guid userId, CancellationToken cancellationToken) =>
        dbContext.StudySessions.AsNoTracking()
            .Where(session => session.UserId == userId
                && session.Status == StudySessionStatus.InProgress)
            .OrderByDescending(session => session.StartedAtUtc)
            .ThenBy(session => session.Id)
            .Select(session => new ActiveStudySessionCandidate(
                session.Id, session.Title,
                session.Items.Count(item =>
                    item.Status == StudySessionItemStatus.Pending
                    || item.Status == StudySessionItemStatus.InProgress),
                null))
            .FirstOrDefaultAsync(cancellationToken);

    private Task<RawStudyPlan?> ReadStudyPlanAsync(
        Guid userId, CancellationToken cancellationToken) =>
        dbContext.StudyPlans.AsNoTracking()
            .Where(plan => plan.UserId == userId
                && (plan.Status == StudyPlanStatus.Ready
                    || plan.Status == StudyPlanStatus.Draft))
            .Where(plan => plan.Items.Any(item =>
                item.ResourceType == StudyPlanResourceType.LearningContent
                    && dbContext.LearningContents.Any(content => content.Id == item.ResourceId
                        && content.Status == ContentStatus.Published
                        && (content.ContentType == LearningContentType.ExternalResource
                            || !dbContext.LearningContentCompletionEvidence.Any(evidence => evidence.UserId == userId && evidence.LearningContentId == content.Id)
                                && !dbContext.LearningContentProgresses.Any(progress => progress.UserId == userId
                                    && progress.LearningContentId == content.Id && progress.Status == LearningProgressStatus.Completed)))
                || item.ResourceType == StudyPlanResourceType.KnowledgeNode
                    && dbContext.KnowledgeNodes.Any(node => node.Id == item.ResourceId && node.UserId == userId && node.Status == KnowledgeNodeStatus.Active)
                || item.ResourceType == StudyPlanResourceType.InterviewQuestion
                    && dbContext.InterviewQuestions.Any(question => question.Id == item.ResourceId && question.UserId == userId && question.Status == InterviewQuestionStatus.Active)
                || item.ResourceType == StudyPlanResourceType.DsaProblem
                    && dbContext.DsaProblems.Any(problem => problem.Id == item.ResourceId && problem.UserId == userId && problem.Status == DsaProblemStatus.Active)))
            .OrderBy(plan => plan.Status == StudyPlanStatus.Ready ? 0 : 1)
            .ThenByDescending(plan => plan.UpdatedAtUtc)
            .ThenByDescending(plan => plan.GeneratedAtUtc)
            .ThenBy(plan => plan.Id)
            .Select(plan => new RawStudyPlan(
                plan.Id, plan.Title, plan.Status, plan.Items.Count,
                plan.Items.Sum(item => item.PlannedDurationMinutes),
                plan.Version,
                true,
                plan.Items.OrderBy(item => item.Position)
                    .ThenBy(item => item.Id).Take(5)
                    .Select(item => new RawStudyPlanItem(
                        item.Id, item.ResourceType, item.ResourceId,
                        item.PlannedDurationMinutes, item.Position))
                    .ToList()))
            .FirstOrDefaultAsync(cancellationToken);

    private Task<List<RawRecommendation>> ReadRecommendationsAsync(
        Guid userId, DateTimeOffset currentUtc,
        CancellationToken cancellationToken) =>
        dbContext.StudyRecommendations.AsNoTracking()
            .Where(item => item.UserId == userId
                && item.Status == RecommendationStatus.Active
                && (item.ExpiresAtUtc == null || item.ExpiresAtUtc > currentUtc))
            .OrderByDescending(item => item.Priority)
            .ThenByDescending(item => item.PriorityScore)
            .ThenByDescending(item => item.GeneratedAtUtc)
            .ThenBy(item => item.Id)
            .Take(RecommendationPreviewLimit)
            .Select(item => new RawRecommendation(
                item.Id, item.ResourceType, item.ResourceId, item.Type,
                item.Priority, item.PriorityScore, item.Reason.WeaknessLevel,
                item.Reason.SignalCount))
            .ToListAsync(cancellationToken);

    private Task<List<RawWeakTopic>> ReadWeakTopicsAsync(
        Guid userId, CancellationToken cancellationToken) =>
        dbContext.WeakTopicProfiles.AsNoTracking()
            .Where(item => item.UserId == userId
                && item.Level != WeaknessLevel.None && item.Score > 0)
            .OrderByDescending(item => item.Level)
            .ThenByDescending(item => item.Score)
            .ThenByDescending(item => item.CalculatedAtUtc)
            .ThenBy(item => item.Id)
            .Take(3)
            .Select(item => new RawWeakTopic(
                item.Id, item.ResourceType, item.ResourceId,
                item.Level, item.Score, item.SignalCount))
            .ToListAsync(cancellationToken);

    private async Task<Dictionary<ResourceKey, ResourceSummary>>
        ReadResourceSummariesAsync(
            Guid userId, RawStudyPlan? plan,
            IReadOnlyCollection<RawRecommendation> recommendations,
            IReadOnlyCollection<RawWeakTopic> weakTopics,
            CancellationToken cancellationToken)
    {
        var references = (plan?.Items.Select(item =>
                new ResourceKey((int)item.ResourceType, item.ResourceId))
                ?? [])
            .Concat(recommendations.Select(item =>
                new ResourceKey((int)item.ResourceType, item.ResourceId)))
            .Concat(weakTopics.Select(item =>
                new ResourceKey((int)item.ResourceType, item.ResourceId)))
            .Distinct()
            .ToArray();
        var result = new Dictionary<ResourceKey, ResourceSummary>();

        var contentIds = Ids(references, 4);
        if (contentIds.Length > 0)
        {
            var rows = await dbContext.LearningContents.AsNoTracking()
                .Where(content => contentIds.Contains(content.Id))
                .Select(content => new ResourceRow(content.Id, content.Title, content.Status == ContentStatus.Published))
                .ToListAsync(cancellationToken);
            Add(result, 4, rows);
        }

        var knowledgeIds = Ids(references, 1);
        if (knowledgeIds.Length > 0)
        {
            var rows = await dbContext.KnowledgeNodes.AsNoTracking()
                .Where(item => item.UserId == userId
                    && knowledgeIds.Contains(item.Id))
                .Select(item => new ResourceRow(
                    item.Id, item.Title,
                    item.Status == KnowledgeNodeStatus.Active))
                .ToListAsync(cancellationToken);
            Add(result, 1, rows);
        }

        var interviewIds = Ids(references, 2);
        if (interviewIds.Length > 0)
        {
            var rows = await dbContext.InterviewQuestions.AsNoTracking()
                .Where(item => item.UserId == userId
                    && interviewIds.Contains(item.Id))
                .Select(item => new ResourceRow(
                    item.Id, item.Title,
                    item.Status == InterviewQuestionStatus.Active))
                .ToListAsync(cancellationToken);
            Add(result, 2, rows);
        }

        var dsaIds = Ids(references, 3);
        if (dsaIds.Length > 0)
        {
            var rows = await dbContext.DsaProblems.AsNoTracking()
                .Where(item => item.UserId == userId
                    && dsaIds.Contains(item.Id))
                .Select(item => new ResourceRow(
                    item.Id, item.Title,
                    item.Status == DsaProblemStatus.Active))
                .ToListAsync(cancellationToken);
            Add(result, 3, rows);
        }

        return result;
    }

    private static TodayStudyPlanReadModel? MapPlan(
        RawStudyPlan? plan, IReadOnlyDictionary<ResourceKey, ResourceSummary> summaries)
    {
        if (plan is null) return null;
        return new TodayStudyPlanReadModel(
            plan.Id, plan.Title, plan.Status, plan.ItemCount,
            plan.TotalMinutes, plan.Version,
            plan.Items.Select(item =>
            {
                var summary = GetSummary(
                    summaries, item.ResourceType, item.ResourceId);
                return new TodayStudyPlanItemReadModel(
                    item.Id, item.ResourceType.ToString(), item.ResourceId,
                    summary.Title, summary.Available,
                    item.PlannedDurationMinutes, item.Position);
            }).ToArray());
    }

    private static ResourceSummary GetSummary<TEnum>(
        IReadOnlyDictionary<ResourceKey, ResourceSummary> summaries,
        TEnum type, Guid resourceId) where TEnum : struct, Enum =>
        summaries.GetValueOrDefault(
            new ResourceKey(
                Convert.ToInt32(type, CultureInfo.InvariantCulture), resourceId),
            new ResourceSummary("Unavailable resource", false));

    private static string RecommendationSummary(RawRecommendation item) =>
        item.WeaknessLevel == WeaknessLevel.Critical
            ? "Critical weak-topic signal needs focused practice."
            : item.SignalCount > 1
                ? $"Repeated learning difficulty across {item.SignalCount} signals."
                : item.Type switch
                {
                    RecommendationType.RetryDsaProblem =>
                        "A recent DSA result suggests another attempt.",
                    RecommendationType.PracticeInterview =>
                        "Interview evidence suggests revisiting this question.",
                    _ => "Review evidence suggests revisiting this topic."
                };

    private static string WeakTopicSummary(RawWeakTopic item) =>
        item.SignalCount > 1
            ? $"Repeated difficulty detected across {item.SignalCount} learning signals."
            : item.Level == WeaknessLevel.Critical
                ? "Critical evidence indicates this topic needs attention."
                : "Recent learning evidence indicates this topic needs practice.";

    private static Guid[] Ids(
        IEnumerable<ResourceKey> references, int kind) =>
        references.Where(item => item.Kind == kind)
            .Select(item => item.Id).Distinct().ToArray();

    private static void Add(
        IDictionary<ResourceKey, ResourceSummary> target, int kind,
        IEnumerable<ResourceRow> rows)
    {
        foreach (var row in rows)
        {
            target[new ResourceKey(kind, row.Id)] =
                new ResourceSummary(row.Title, row.Available);
        }
    }

    private static WeekRange GetCurrentWeek(DateTimeOffset currentUtc)
    {
        var currentDate = DateOnly.FromDateTime(currentUtc.UtcDateTime);
        var daysSinceMonday = ((int)currentDate.DayOfWeek + 6) % 7;
        var startDate = currentDate.AddDays(-daysSinceMonday);
        var startUtc = new DateTimeOffset(
            startDate.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);
        return new WeekRange(startDate, startUtc, startUtc.AddDays(7));
    }

    private sealed record WeekRange(
        DateOnly StartDate, DateTimeOffset StartUtc, DateTimeOffset EndUtc);
    private sealed record RawStudyPlan(
        Guid Id, string Title, StudyPlanStatus Status, int ItemCount,
        int TotalMinutes, int Version, bool HasActionableItems, IReadOnlyList<RawStudyPlanItem> Items);
    private sealed record RawStudyPlanItem(
        Guid Id, StudyPlanResourceType ResourceType, Guid ResourceId,
        int PlannedDurationMinutes, int Position);
    private sealed record RawRecommendation(
        Guid Id, RecommendationResourceType ResourceType, Guid ResourceId,
        RecommendationType Type, RecommendationPriority Priority,
        decimal PriorityScore, WeaknessLevel WeaknessLevel, int SignalCount);
    private sealed record RawWeakTopic(
        Guid Id, WeakTopicResourceType ResourceType, Guid ResourceId,
        WeaknessLevel Level, decimal Score, int SignalCount);
    private sealed record ResourceKey(int Kind, Guid Id);
    private sealed record ResourceSummary(string Title, bool Available);
    private sealed record ResourceRow(Guid Id, string Title, bool Available);
}
