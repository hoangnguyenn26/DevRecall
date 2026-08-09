using DevRecall.Application.Common.Exceptions;
using DevRecall.Application.Interview.Practice;
using DevRecall.Domain.Interview.Practice;
using DevRecall.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace DevRecall.Infrastructure.Interview.Practice;

internal sealed class InterviewPracticeAttemptRepository(DevRecallDbContext dbContext)
    : IInterviewPracticeAttemptRepository
{
    public Task<InterviewPracticeAttempt?> GetBySubmissionIdAsync(
        Guid userId, Guid submissionId, CancellationToken cancellationToken) =>
        dbContext.InterviewPracticeAttempts.Include(item => item.FollowUps)
            .SingleOrDefaultAsync(item => item.UserId == userId && item.SubmissionId == submissionId, cancellationToken);

    public async Task<IReadOnlyList<InterviewPracticeAttempt>> GetRecentAsync(
        Guid userId, Guid questionId, int take, CancellationToken cancellationToken) =>
        await dbContext.InterviewPracticeAttempts.AsNoTracking()
            .Include(item => item.FollowUps)
            .Where(item => item.UserId == userId && item.QuestionId == questionId)
            .OrderByDescending(item => item.CompletedAtUtc).ThenByDescending(item => item.Id)
            .Take(take).ToListAsync(cancellationToken);

    public void Add(InterviewPracticeAttempt attempt) => dbContext.InterviewPracticeAttempts.Add(attempt);

    public async Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        try { await dbContext.SaveChangesAsync(cancellationToken); }
        catch (DbUpdateException exception) when (exception.InnerException is PostgresException { ConstraintName: "ux_interview_practice_attempts_user_submission" })
        {
            throw new ConflictException("INTERVIEW_PRACTICE_SUBMISSION_CONFLICT", "This practice submission was already recorded.");
        }
    }
}
