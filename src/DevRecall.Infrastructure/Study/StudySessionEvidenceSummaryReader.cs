using DevRecall.Application.Study.GetDetail;
using DevRecall.Domain.Study;
using DevRecall.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace DevRecall.Infrastructure.Study;

internal sealed class StudySessionEvidenceSummaryReader(
    DevRecallDbContext dbContext) : IStudySessionEvidenceSummaryReader
{
    public async Task<IReadOnlyDictionary<Guid, StudySessionEvidenceSummary>> ReadManyAsync(
        Guid userId, IReadOnlyCollection<StudySessionEvidenceReference> evidence,
        CancellationToken cancellationToken)
    {
        var result = new Dictionary<Guid, StudySessionEvidenceSummary>();
        var interview = evidence.Where(item =>
            item.ResourceType == StudyResourceType.InterviewQuestion).ToArray();
        if (interview.Length > 0)
        {
            var ids = interview.Select(item => item.EvidenceId).ToArray();
            var attempts = await dbContext.InterviewPracticeAttempts.AsNoTracking()
                .Where(item => item.UserId == userId && ids.Contains(item.Id))
                .Select(item => new
                {
                    item.Id, item.SelfRating, item.DurationSeconds
                }).ToListAsync(cancellationToken);
            foreach (var reference in interview)
            {
                var attempt = attempts.SingleOrDefault(item =>
                    item.Id == reference.EvidenceId);
                if (attempt is not null)
                {
                    result[reference.ItemId] = new(
                        attempt.Id, "InterviewAttempt",
                        attempt.SelfRating.ToString(), attempt.DurationSeconds,
                        null);
                }
            }
        }

        var dsa = evidence.Where(item =>
            item.ResourceType == StudyResourceType.DsaProblem).ToArray();
        if (dsa.Length > 0)
        {
            var ids = dsa.Select(item => item.EvidenceId).ToArray();
            var attempts = await (
                from attempt in dbContext.DsaAttempts.AsNoTracking()
                join problem in dbContext.DsaProblems.AsNoTracking()
                    on attempt.DsaProblemId equals problem.Id
                where problem.UserId == userId && ids.Contains(attempt.Id)
                select new
                {
                    attempt.Id, attempt.Result, attempt.DurationMinutes,
                    attempt.TimeComplexity
                }).ToListAsync(cancellationToken);
            foreach (var reference in dsa)
            {
                var attempt = attempts.SingleOrDefault(item =>
                    item.Id == reference.EvidenceId);
                if (attempt is not null)
                {
                    result[reference.ItemId] = new(
                        attempt.Id, "DsaAttempt", attempt.Result.ToString(),
                        checked(attempt.DurationMinutes * 60),
                        attempt.TimeComplexity);
                }
            }
        }

        return result;
    }
}
