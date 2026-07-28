using DevRecall.Application.Reviews.Resources;
using DevRecall.Domain.Interview;
using DevRecall.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace DevRecall.Infrastructure.Reviews.Resources;

internal sealed class ReviewInterviewResourceReader(DevRecallDbContext dbContext)
    : IReviewInterviewResourceReader
{
    public async Task<ReviewSourceResource?> FindAsync(
        Guid userId, Guid resourceId, CancellationToken cancellationToken)
    {
        var row = await dbContext.InterviewQuestions
            .AsNoTracking()
            .Where(question =>
                question.Id == resourceId && question.UserId == userId)
            .Select(question => new
            {
                question.Id,
                question.Title,
                question.Question,
                question.Status
            })
            .SingleOrDefaultAsync(cancellationToken);

        return row is null
            ? null
            : new ReviewSourceResource(
                row.Id, row.Title, ReviewPreviewBuilder.Build(row.Question),
                row.Status == InterviewQuestionStatus.Archived);
    }
}
