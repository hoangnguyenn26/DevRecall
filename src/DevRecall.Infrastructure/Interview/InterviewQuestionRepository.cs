using DevRecall.Application.Common.Pagination;
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

    public async Task<PagedReadResult<InterviewQuestionListReadItem>>
        GetActiveListAsync(
            Guid userId,
            string? topic,
            InterviewQuestionDifficulty? difficulty,
            int skip,
            int take,
            CancellationToken cancellationToken)
    {
        var query = dbContext.InterviewQuestions
            .AsNoTracking()
            .Where(question => question.UserId == userId
                && question.Status == InterviewQuestionStatus.Active);

        if (topic is not null)
        {
            query = query.Where(question =>
                EF.Functions.ILike(question.Topic, topic));
        }

        if (difficulty is not null)
        {
            query = query.Where(question =>
                question.Difficulty == difficulty.Value);
        }

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(question => question.UpdatedAtUtc)
            .ThenBy(question => question.Title)
            .ThenBy(question => question.Id)
            .Skip(skip)
            .Take(take)
            .Select(question => new InterviewQuestionListReadItem(
                question.Id, question.Title, question.Topic,
                question.Difficulty, question.UpdatedAtUtc))
            .ToListAsync(cancellationToken);

        return new PagedReadResult<InterviewQuestionListReadItem>(
            items,
            totalCount);
    }

    public void Add(InterviewQuestion question) =>
        dbContext.InterviewQuestions.Add(question);

    public Task SaveChangesAsync(CancellationToken cancellationToken) =>
        dbContext.SaveChangesAsync(cancellationToken);
}
