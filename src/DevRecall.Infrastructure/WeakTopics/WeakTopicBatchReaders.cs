using DevRecall.Application.WeakTopics.RecalculateAll;
using DevRecall.Domain.Dsa.Attempts;
using DevRecall.Domain.Reviews;
using DevRecall.Domain.Study;
using DevRecall.Domain.WeakTopics;
using DevRecall.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace DevRecall.Infrastructure.WeakTopics;

internal sealed class WeakTopicCandidateReader(DevRecallDbContext dbContext)
    : IWeakTopicCandidateReader
{
    public async Task<IReadOnlyList<WeakTopicCandidate>> ReadAsync(
        Guid userId, DateTimeOffset fromUtc, DateTimeOffset toUtc,
        CancellationToken cancellationToken)
    {
        var candidates = await dbContext.WeakTopicProfiles.AsNoTracking()
            .Where(x => x.UserId == userId)
            .Select(x => new WeakTopicCandidate(x.ResourceType, x.ResourceId))
            .ToListAsync(cancellationToken);
        var reviews = await (
            from history in dbContext.ReviewHistories.AsNoTracking()
            join item in dbContext.ReviewItems.AsNoTracking()
                on history.ReviewItemId equals item.Id
            where item.UserId == userId && history.ReviewedAtUtc >= fromUtc
                && history.ReviewedAtUtc < toUtc
            select new { item.ResourceType, item.ResourceId })
            .Distinct().ToListAsync(cancellationToken);
        candidates.AddRange(reviews.Select(x =>
            new WeakTopicCandidate(Map(x.ResourceType), x.ResourceId)));
        var studies = await (
            from item in dbContext.StudySessionItems.AsNoTracking()
            join session in dbContext.StudySessions.AsNoTracking()
                on item.StudySessionId equals session.Id
            where session.UserId == userId && item.CompletedAtUtc >= fromUtc
                && item.CompletedAtUtc < toUtc
                && item.ResourceType != StudyResourceType.ReviewItem
                && (item.Status == StudySessionItemStatus.Completed
                    || item.Status == StudySessionItemStatus.Skipped)
            select new { item.ResourceType, item.ResourceId })
            .Distinct().ToListAsync(cancellationToken);
        candidates.AddRange(studies.Select(x =>
            new WeakTopicCandidate(Map(x.ResourceType), x.ResourceId)));
        var dsa = await (
            from attempt in dbContext.DsaAttempts.AsNoTracking()
            join problem in dbContext.DsaProblems.AsNoTracking()
                on attempt.DsaProblemId equals problem.Id
            where problem.UserId == userId && attempt.AttemptedAtUtc >= fromUtc
                && attempt.AttemptedAtUtc < toUtc
            select attempt.DsaProblemId).Distinct().ToListAsync(cancellationToken);
        candidates.AddRange(dsa.Select(x =>
            new WeakTopicCandidate(WeakTopicResourceType.DsaProblem, x)));
        return candidates.Distinct().OrderBy(x => x.ResourceType)
            .ThenBy(x => x.ResourceId).ToArray();
    }

    internal static WeakTopicResourceType Map(ReviewResourceType type) => type switch
    {
        ReviewResourceType.KnowledgeNode => WeakTopicResourceType.KnowledgeNode,
        ReviewResourceType.InterviewQuestion => WeakTopicResourceType.InterviewQuestion,
        ReviewResourceType.DsaProblem => WeakTopicResourceType.DsaProblem,
        _ => throw new ArgumentOutOfRangeException(nameof(type))
    };

    internal static WeakTopicResourceType Map(StudyResourceType type) => type switch
    {
        StudyResourceType.KnowledgeNode => WeakTopicResourceType.KnowledgeNode,
        StudyResourceType.InterviewQuestion => WeakTopicResourceType.InterviewQuestion,
        StudyResourceType.DsaProblem => WeakTopicResourceType.DsaProblem,
        _ => throw new ArgumentOutOfRangeException(nameof(type))
    };
}

internal sealed class WeakTopicBatchSignalReader(DevRecallDbContext dbContext)
    : IWeakTopicBatchSignalReader
{
    public async Task<IReadOnlyList<WeakTopicBatchSignalReadModel>> ReadAsync(
        Guid userId, DateTimeOffset fromUtc, DateTimeOffset toUtc,
        CancellationToken cancellationToken)
    {
        var result = new List<WeakTopicBatchSignalReadModel>();
        var reviews = await (
            from history in dbContext.ReviewHistories.AsNoTracking()
            join item in dbContext.ReviewItems.AsNoTracking()
                on history.ReviewItemId equals item.Id
            where item.UserId == userId && history.ReviewedAtUtc >= fromUtc
                && history.ReviewedAtUtc < toUtc
            select new
            {
                item.ResourceType,
                item.ResourceId,
                history.Evaluation,
                history.ReviewedAtUtc
            }).ToListAsync(cancellationToken);
        result.AddRange(reviews.Select(x => new WeakTopicBatchSignalReadModel(
            WeakTopicCandidateReader.Map(x.ResourceType), x.ResourceId,
            Map(x.Evaluation), x.ReviewedAtUtc)));
        var studies = await (
            from item in dbContext.StudySessionItems.AsNoTracking()
            join session in dbContext.StudySessions.AsNoTracking()
                on item.StudySessionId equals session.Id
            where session.UserId == userId && item.CompletedAtUtc >= fromUtc
                && item.CompletedAtUtc < toUtc
                && item.ResourceType != StudyResourceType.ReviewItem
                && (item.Status == StudySessionItemStatus.Completed
                    || item.Status == StudySessionItemStatus.Skipped)
            select new
            {
                item.ResourceType,
                item.ResourceId,
                item.Status,
                item.CompletedAtUtc
            }).ToListAsync(cancellationToken);
        result.AddRange(studies.Select(x => new WeakTopicBatchSignalReadModel(
            WeakTopicCandidateReader.Map(x.ResourceType), x.ResourceId,
            x.Status == StudySessionItemStatus.Completed
                ? WeaknessSignalType.StudyItemCompleted
                : WeaknessSignalType.StudyItemSkipped,
            x.CompletedAtUtc!.Value)));
        var dsa = await (
            from attempt in dbContext.DsaAttempts.AsNoTracking()
            join problem in dbContext.DsaProblems.AsNoTracking()
                on attempt.DsaProblemId equals problem.Id
            where problem.UserId == userId && attempt.AttemptedAtUtc >= fromUtc
                && attempt.AttemptedAtUtc < toUtc
            select new
            {
                attempt.DsaProblemId,
                attempt.Result,
                attempt.AttemptedAtUtc
            }).ToListAsync(cancellationToken);
        result.AddRange(dsa.Select(x => new WeakTopicBatchSignalReadModel(
            WeakTopicResourceType.DsaProblem, x.DsaProblemId,
            Map(x.Result), x.AttemptedAtUtc)));
        return result;
    }

    private static WeaknessSignalType Map(ReviewEvaluation value) => value switch
    {
        ReviewEvaluation.Again => WeaknessSignalType.ReviewAgain,
        ReviewEvaluation.Hard => WeaknessSignalType.ReviewHard,
        ReviewEvaluation.Good => WeaknessSignalType.ReviewGood,
        ReviewEvaluation.Easy => WeaknessSignalType.ReviewEasy,
        _ => throw new ArgumentOutOfRangeException(nameof(value))
    };

    private static WeaknessSignalType Map(DsaAttemptResult value) => value switch
    {
        DsaAttemptResult.Solved => WeaknessSignalType.DsaSolved,
        DsaAttemptResult.PartiallySolved => WeaknessSignalType.DsaPartiallySolved,
        DsaAttemptResult.Failed => WeaknessSignalType.DsaFailed,
        DsaAttemptResult.Skipped => WeaknessSignalType.DsaSkipped,
        _ => throw new ArgumentOutOfRangeException(nameof(value))
    };
}
