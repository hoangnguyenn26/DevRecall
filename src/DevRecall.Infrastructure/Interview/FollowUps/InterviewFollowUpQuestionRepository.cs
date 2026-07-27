using DevRecall.Application.Interview.FollowUps;
using DevRecall.Domain.Interview.FollowUps;
using DevRecall.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace DevRecall.Infrastructure.Interview.FollowUps;

internal sealed class InterviewFollowUpQuestionRepository(
    DevRecallDbContext dbContext)
    : IInterviewFollowUpQuestionRepository
{
    public Task<InterviewFollowUpQuestion?> GetByIdAndQuestionIdAsync(
        Guid id, Guid interviewQuestionId,
        CancellationToken cancellationToken) =>
        dbContext.InterviewFollowUpQuestions.SingleOrDefaultAsync(
            followUp => followUp.Id == id
                && followUp.InterviewQuestionId == interviewQuestionId,
            cancellationToken);

    public async Task<IReadOnlyList<InterviewFollowUpQuestion>>
        GetActiveByQuestionIdAsync(
            Guid interviewQuestionId, bool trackChanges,
            CancellationToken cancellationToken)
    {
        IQueryable<InterviewFollowUpQuestion> query =
            dbContext.InterviewFollowUpQuestions;

        if (!trackChanges)
        {
            query = query.AsNoTracking();
        }

        return await query
            .Where(followUp =>
                followUp.InterviewQuestionId == interviewQuestionId
                && followUp.Status == InterviewFollowUpQuestionStatus.Active)
            .OrderBy(followUp => followUp.SortOrder)
            .ThenBy(followUp => followUp.Id)
            .ToListAsync(cancellationToken);
    }

    public Task<int> CountActiveAsync(
        Guid interviewQuestionId,
        CancellationToken cancellationToken) =>
        dbContext.InterviewFollowUpQuestions
            .AsNoTracking()
            .CountAsync(
                followUp =>
                    followUp.InterviewQuestionId == interviewQuestionId
                    && followUp.Status
                        == InterviewFollowUpQuestionStatus.Active,
                cancellationToken);

    public void Add(InterviewFollowUpQuestion followUp) =>
        dbContext.InterviewFollowUpQuestions.Add(followUp);

    public Task SaveChangesAsync(CancellationToken cancellationToken) =>
        dbContext.SaveChangesAsync(cancellationToken);
}
