using DevRecall.Application.WeakTopics.Signals;
using DevRecall.Domain.Dsa.Attempts;
using DevRecall.Domain.Reviews;
using DevRecall.Domain.Study;
using DevRecall.Domain.WeakTopics;
using DevRecall.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace DevRecall.Infrastructure.WeakTopics;

internal sealed class WeakTopicSignalReader(DevRecallDbContext dbContext)
    : IWeakTopicSignalReader
{
    public async Task<IReadOnlyList<WeakTopicSignalReadModel>> ReadAsync(
        Guid userId, WeakTopicResourceType resourceType, Guid resourceId,
        DateTimeOffset fromUtc, DateTimeOffset toUtc,
        CancellationToken cancellationToken)
    {
        var signals = new List<WeakTopicSignalReadModel>();
        signals.AddRange(await ReadReviewSignalsAsync(
            userId, resourceType, resourceId, fromUtc, toUtc,
            cancellationToken));
        signals.AddRange(await ReadStudySignalsAsync(
            userId, resourceType, resourceId, fromUtc, toUtc,
            cancellationToken));
        if (resourceType == WeakTopicResourceType.DsaProblem)
        {
            signals.AddRange(await ReadDsaSignalsAsync(
                userId, resourceId, fromUtc, toUtc, cancellationToken));
        }

        return signals.OrderBy(signal => signal.OccurredAtUtc).ToList();
    }

    private async Task<IReadOnlyList<WeakTopicSignalReadModel>>
        ReadReviewSignalsAsync(
            Guid userId, WeakTopicResourceType resourceType, Guid resourceId,
            DateTimeOffset fromUtc, DateTimeOffset toUtc,
            CancellationToken cancellationToken)
    {
        var reviewType = MapReviewResourceType(resourceType);
        var rows = await (
            from history in dbContext.ReviewHistories.AsNoTracking()
            join item in dbContext.ReviewItems.AsNoTracking()
                on history.ReviewItemId equals item.Id
            where item.UserId == userId
                && item.ResourceType == reviewType
                && item.ResourceId == resourceId
                && history.ReviewedAtUtc >= fromUtc
                && history.ReviewedAtUtc < toUtc
            select new { history.Evaluation, history.ReviewedAtUtc })
            .ToListAsync(cancellationToken);
        return rows.Select(row => new WeakTopicSignalReadModel(
            MapEvaluation(row.Evaluation), row.ReviewedAtUtc)).ToList();
    }

    private async Task<IReadOnlyList<WeakTopicSignalReadModel>>
        ReadStudySignalsAsync(
            Guid userId, WeakTopicResourceType resourceType, Guid resourceId,
            DateTimeOffset fromUtc, DateTimeOffset toUtc,
            CancellationToken cancellationToken)
    {
        var studyType = MapStudyResourceType(resourceType);
        var rows = await (
            from item in dbContext.StudySessionItems.AsNoTracking()
            join session in dbContext.StudySessions.AsNoTracking()
                on item.StudySessionId equals session.Id
            where session.UserId == userId
                && item.ResourceType == studyType
                && item.ResourceId == resourceId
                && item.CompletedAtUtc >= fromUtc
                && item.CompletedAtUtc < toUtc
                && (item.Status == StudySessionItemStatus.Completed
                    || item.Status == StudySessionItemStatus.Skipped)
            select new { item.Status, item.CompletedAtUtc })
            .ToListAsync(cancellationToken);
        return rows.Select(row => new WeakTopicSignalReadModel(
            MapStudyStatus(row.Status), row.CompletedAtUtc!.Value)).ToList();
    }

    private async Task<IReadOnlyList<WeakTopicSignalReadModel>>
        ReadDsaSignalsAsync(
            Guid userId, Guid resourceId,
            DateTimeOffset fromUtc, DateTimeOffset toUtc,
            CancellationToken cancellationToken)
    {
        var rows = await (
            from attempt in dbContext.DsaAttempts.AsNoTracking()
            join problem in dbContext.DsaProblems.AsNoTracking()
                on attempt.DsaProblemId equals problem.Id
            where problem.UserId == userId
                && problem.Id == resourceId
                && attempt.AttemptedAtUtc >= fromUtc
                && attempt.AttemptedAtUtc < toUtc
            select new { attempt.Result, attempt.AttemptedAtUtc })
            .ToListAsync(cancellationToken);
        return rows.Select(row => new WeakTopicSignalReadModel(
            MapDsaResult(row.Result), row.AttemptedAtUtc)).ToList();
    }

    private static ReviewResourceType MapReviewResourceType(
        WeakTopicResourceType type) =>
        type switch
        {
            WeakTopicResourceType.KnowledgeNode =>
                ReviewResourceType.KnowledgeNode,
            WeakTopicResourceType.InterviewQuestion =>
                ReviewResourceType.InterviewQuestion,
            WeakTopicResourceType.DsaProblem => ReviewResourceType.DsaProblem,
            _ => throw new ArgumentOutOfRangeException(nameof(type))
        };

    private static StudyResourceType MapStudyResourceType(
        WeakTopicResourceType type) =>
        type switch
        {
            WeakTopicResourceType.KnowledgeNode =>
                StudyResourceType.KnowledgeNode,
            WeakTopicResourceType.InterviewQuestion =>
                StudyResourceType.InterviewQuestion,
            WeakTopicResourceType.DsaProblem => StudyResourceType.DsaProblem,
            _ => throw new ArgumentOutOfRangeException(nameof(type))
        };

    private static WeaknessSignalType MapEvaluation(
        ReviewEvaluation evaluation) =>
        evaluation switch
        {
            ReviewEvaluation.Again => WeaknessSignalType.ReviewAgain,
            ReviewEvaluation.Hard => WeaknessSignalType.ReviewHard,
            ReviewEvaluation.Good => WeaknessSignalType.ReviewGood,
            ReviewEvaluation.Easy => WeaknessSignalType.ReviewEasy,
            _ => throw new ArgumentOutOfRangeException(nameof(evaluation))
        };

    private static WeaknessSignalType MapStudyStatus(
        StudySessionItemStatus status) =>
        status switch
        {
            StudySessionItemStatus.Completed =>
                WeaknessSignalType.StudyItemCompleted,
            StudySessionItemStatus.Skipped =>
                WeaknessSignalType.StudyItemSkipped,
            _ => throw new ArgumentOutOfRangeException(nameof(status))
        };

    private static WeaknessSignalType MapDsaResult(DsaAttemptResult result) =>
        result switch
        {
            DsaAttemptResult.Solved => WeaknessSignalType.DsaSolved,
            DsaAttemptResult.PartiallySolved =>
                WeaknessSignalType.DsaPartiallySolved,
            DsaAttemptResult.Failed => WeaknessSignalType.DsaFailed,
            DsaAttemptResult.Skipped => WeaknessSignalType.DsaSkipped,
            _ => throw new ArgumentOutOfRangeException(nameof(result))
        };
}
