using DevRecall.Application.Today;
using DevRecall.Domain.Dsa;
using DevRecall.Domain.Interview;
using DevRecall.Domain.Knowledge;
using DevRecall.Domain.Study;
using DevRecall.Domain.StudyPlans;
using DevRecall.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace DevRecall.Infrastructure.Today;

internal sealed class TodayRecentActivityReader(DevRecallDbContext dbContext) : ITodayRecentActivityReader
{
    public async Task<TodayRecentActivityReadModel?> ReadLatestAsync(
        Guid userId, DateTimeOffset currentUtc, CancellationToken cancellationToken)
    {
        var cutoff = currentUtc.AddDays(-14);
        var candidates = new List<TodayRecentActivityReadModel?>
        {
            await dbContext.KnowledgeNodes.AsNoTracking()
                .Where(x => x.UserId == userId && x.Status == KnowledgeNodeStatus.Active && x.UpdatedAtUtc >= cutoff)
                .OrderByDescending(x => x.UpdatedAtUtc).ThenBy(x => x.Id)
                .Select(x => new TodayRecentActivityReadModel(TodayRecentActivityType.Knowledge, x.Id, x.Title,
                    "Return to your recently updated knowledge.", x.UpdatedAtUtc, $"/app/knowledge/{x.Id}", "book-open"))
                .FirstOrDefaultAsync(cancellationToken),
            await (from answer in dbContext.InterviewAnswerVersions.AsNoTracking()
                   join question in dbContext.InterviewQuestions.AsNoTracking() on answer.InterviewQuestionId equals question.Id
                   where question.UserId == userId && question.Status == InterviewQuestionStatus.Active && answer.UpdatedAtUtc >= cutoff
                   orderby answer.UpdatedAtUtc descending, question.Id
                   select new TodayRecentActivityReadModel(TodayRecentActivityType.InterviewPractice, question.Id, question.Title,
                       "Continue refining your interview answer.", answer.UpdatedAtUtc, $"/app/interview/{question.Id}", "messages-square"))
                .FirstOrDefaultAsync(cancellationToken),
            await (from attempt in dbContext.DsaAttempts.AsNoTracking()
                   join problem in dbContext.DsaProblems.AsNoTracking() on attempt.DsaProblemId equals problem.Id
                   where problem.UserId == userId && problem.Status == DsaProblemStatus.Active && attempt.AttemptedAtUtc >= cutoff
                   orderby attempt.AttemptedAtUtc descending, problem.Id
                   select new TodayRecentActivityReadModel(TodayRecentActivityType.DsaAttempt, problem.Id, problem.Title,
                       "Revisit your latest DSA attempt.", attempt.AttemptedAtUtc, $"/app/dsa/{problem.Id}", "code-2"))
                .FirstOrDefaultAsync(cancellationToken),
            await dbContext.StudyPlans.AsNoTracking()
                .Where(x => x.UserId == userId && (x.Status == StudyPlanStatus.Draft || x.Status == StudyPlanStatus.Ready) && x.UpdatedAtUtc >= cutoff)
                .OrderByDescending(x => x.UpdatedAtUtc).ThenBy(x => x.Id)
                .Select(x => new TodayRecentActivityReadModel(TodayRecentActivityType.StudyPlan, x.Id, x.Title,
                    "Continue shaping your study plan.", x.UpdatedAtUtc, $"/app/study-plans/{x.Id}", "list-checks"))
                .FirstOrDefaultAsync(cancellationToken),
            await dbContext.StudySessions.AsNoTracking()
                .Where(x => x.UserId == userId && (x.Status == StudySessionStatus.Planned || x.Status == StudySessionStatus.InProgress) && x.UpdatedAtUtc >= cutoff)
                .OrderByDescending(x => x.UpdatedAtUtc).ThenBy(x => x.Id)
                .Select(x => new TodayRecentActivityReadModel(TodayRecentActivityType.StudySession, x.Id, x.Title,
                    "Return to your study session.", x.UpdatedAtUtc, $"/app/study-sessions/{x.Id}", "timer"))
                .FirstOrDefaultAsync(cancellationToken)
        };

        return candidates.Where(x => x is not null).Select(x => x!)
            .OrderByDescending(x => x.OccurredAtUtc).ThenBy(x => x.Type).ThenBy(x => x.ResourceId)
            .FirstOrDefault();
    }
}
