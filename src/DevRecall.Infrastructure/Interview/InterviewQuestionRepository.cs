using DevRecall.Application.Interview;
using DevRecall.Domain.Interview;
using DevRecall.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace DevRecall.Infrastructure.Interview;

internal sealed class InterviewQuestionRepository(DevRecallDbContext dbContext)
    : IInterviewQuestionRepository
{
    public Task<InterviewQuestion?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken) =>
        dbContext.InterviewQuestions.SingleOrDefaultAsync(
            question => question.Id == id,
            cancellationToken);

    public Task<InterviewQuestion?> GetByIdAndUserIdAsync(
        Guid id,
        Guid userId,
        CancellationToken cancellationToken) =>
        dbContext.InterviewQuestions
            .AsNoTracking()
            .SingleOrDefaultAsync(
                question => question.Id == id && question.UserId == userId,
                cancellationToken);

    public void Add(InterviewQuestion question) =>
        dbContext.InterviewQuestions.Add(question);

    public Task SaveChangesAsync(CancellationToken cancellationToken) =>
        dbContext.SaveChangesAsync(cancellationToken);
}
