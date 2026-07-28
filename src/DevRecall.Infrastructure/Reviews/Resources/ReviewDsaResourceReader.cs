using DevRecall.Application.Reviews.Resources;
using DevRecall.Domain.Dsa;
using DevRecall.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace DevRecall.Infrastructure.Reviews.Resources;

internal sealed class ReviewDsaResourceReader(DevRecallDbContext dbContext)
    : IReviewDsaResourceReader
{
    public async Task<ReviewSourceResource?> FindAsync(
        Guid userId, Guid resourceId, CancellationToken cancellationToken)
    {
        var row = await dbContext.DsaProblems
            .AsNoTracking()
            .Where(problem =>
                problem.Id == resourceId && problem.UserId == userId)
            .Select(problem => new
            {
                problem.Id,
                problem.Title,
                problem.Description,
                problem.Status
            })
            .SingleOrDefaultAsync(cancellationToken);

        return row is null
            ? null
            : new ReviewSourceResource(
                row.Id, row.Title, ReviewPreviewBuilder.Build(row.Description),
                row.Status == DsaProblemStatus.Archived);
    }
}
