using DevRecall.Domain.LearningContent;
using DevRecall.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace DevRecall.Infrastructure.Development;

public sealed record LearningContentConsistencyReport(
    int CompletedProgressWithoutEvidence,
    int EvidenceWithoutCompletedProgress,
    int RepairedProgresses,
    int RepairedEvidence);

public sealed class LearningContentConsistencyRepair(DevRecallDbContext dbContext)
{
    public static void EnsureDevelopmentEnvironment(bool isDevelopment)
    {
        if (!isDevelopment)
            throw new InvalidOperationException(
                "Learning Content consistency repair can only run in the Development environment.");
    }

    public async Task<LearningContentConsistencyReport> RepairAsync(
        CancellationToken cancellationToken = default)
    {
        var progresses = await dbContext.LearningContentProgresses
            .ToListAsync(cancellationToken);
        var evidence = await dbContext.LearningContentCompletionEvidence
            .ToListAsync(cancellationToken);
        var evidenceByKey = evidence.ToDictionary(item => (item.UserId, item.LearningContentId));
        var progressByKey = progresses.ToDictionary(item => (item.UserId, item.LearningContentId));
        var missingEvidence = progresses.Where(progress =>
            progress.Status == LearningProgressStatus.Completed
            && !evidenceByKey.ContainsKey((progress.UserId, progress.LearningContentId))).ToArray();
        var evidenceWithoutCompletion = evidence.Where(item =>
            !progressByKey.TryGetValue((item.UserId, item.LearningContentId), out var progress)
            || progress.Status != LearningProgressStatus.Completed).ToArray();
        var contentIds = missingEvidence.Select(item => item.LearningContentId).Distinct().ToArray();
        var titles = await dbContext.LearningContents.AsNoTracking()
            .Where(item => contentIds.Contains(item.Id))
            .ToDictionaryAsync(item => item.Id, item => item.Title, cancellationToken);

        foreach (var progress in missingEvidence)
        {
            if (progress.CompletedAtUtc is null || !titles.TryGetValue(progress.LearningContentId, out var title))
                throw new InvalidOperationException(
                    $"Cannot repair completed progress {progress.Id}: trusted completion data is unavailable.");
            dbContext.LearningContentCompletionEvidence.Add(
                LearningContentCompletionEvidence.Create(Guid.NewGuid(), progress.UserId,
                    progress.LearningContentId, title, progress.CompletedAtUtc.Value));
        }

        var repairedProgresses = 0;
        foreach (var item in evidenceWithoutCompletion)
        {
            if (!progressByKey.TryGetValue((item.UserId, item.LearningContentId), out var progress))
            {
                dbContext.LearningContentProgresses.Add(LearningContentProgress.CompleteDirectly(
                    Guid.NewGuid(), item.UserId, item.LearningContentId, item.CompletedAtUtc));
            }
            else
            {
                progress.Complete(progress.Version, item.CompletedAtUtc);
            }
            repairedProgresses++;
        }

        if (dbContext.ChangeTracker.HasChanges())
            await dbContext.SaveChangesAsync(cancellationToken);
        return new(missingEvidence.Length, evidenceWithoutCompletion.Length,
            repairedProgresses, missingEvidence.Length);
    }
}
