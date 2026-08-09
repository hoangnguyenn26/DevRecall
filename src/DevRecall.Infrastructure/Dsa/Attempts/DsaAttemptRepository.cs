using DevRecall.Application.Common.Exceptions;
using DevRecall.Application.Common.Pagination;
using DevRecall.Application.Dsa.Attempts;
using DevRecall.Domain.Dsa.Attempts;
using DevRecall.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace DevRecall.Infrastructure.Dsa.Attempts;

internal sealed class DsaAttemptRepository(DevRecallDbContext dbContext)
    : IDsaAttemptRepository
{
    public async Task<(DsaPracticeSubmission Submission, DsaAttempt Attempt)?> GetPracticeSubmissionAsync(
        Guid userId, Guid submissionId, CancellationToken cancellationToken)
    {
        var item = await dbContext.DsaPracticeSubmissions.AsNoTracking()
            .Where(submission => submission.UserId == userId && submission.SubmissionId == submissionId)
            .Join(dbContext.DsaAttempts.AsNoTracking(), submission => submission.DsaAttemptId,
                attempt => attempt.Id, (submission, attempt) => new { submission, attempt })
            .SingleOrDefaultAsync(cancellationToken);
        return item is null ? null : (item.submission, item.attempt);
    }

    public Task<DsaAttempt?> GetByIdAndProblemIdAsync(
        Guid id, Guid dsaProblemId, CancellationToken cancellationToken) =>
        dbContext.DsaAttempts
            .AsNoTracking()
            .SingleOrDefaultAsync(
                attempt => attempt.Id == id
                    && attempt.DsaProblemId == dsaProblemId,
                cancellationToken);

    public async Task<int> GetNextAttemptNumberAsync(
        Guid dsaProblemId, CancellationToken cancellationToken)
    {
        var maximumAttemptNumber = await dbContext.DsaAttempts
            .AsNoTracking()
            .Where(attempt => attempt.DsaProblemId == dsaProblemId)
            .Select(attempt => (int?)attempt.AttemptNumber)
            .MaxAsync(cancellationToken);
        return maximumAttemptNumber is null
            ? 1
            : maximumAttemptNumber.Value + 1;
    }

    public async Task<PagedReadResult<DsaAttemptListReadItem>> GetListAsync(
        Guid dsaProblemId, DsaAttemptResult? result, int skip, int take,
        CancellationToken cancellationToken)
    {
        var query = dbContext.DsaAttempts
            .AsNoTracking()
            .Where(attempt => attempt.DsaProblemId == dsaProblemId);
        if (result is not null)
        {
            query = query.Where(attempt => attempt.Result == result.Value);
        }

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(attempt => attempt.AttemptNumber)
            .ThenBy(attempt => attempt.Id)
            .Skip(skip)
            .Take(take)
            .Select(attempt => new DsaAttemptListReadItem(
                attempt.Id, attempt.AttemptNumber, attempt.Result,
                attempt.Language, attempt.TimeComplexity,
                attempt.SpaceComplexity, attempt.DurationMinutes,
                attempt.AttemptedAtUtc, attempt.CreatedAtUtc))
            .ToListAsync(cancellationToken);
        return new PagedReadResult<DsaAttemptListReadItem>(
            items, totalCount);
    }

    public Task<DsaAttempt?> GetLatestSuccessfulAsync(
        Guid dsaProblemId, CancellationToken cancellationToken) =>
        dbContext.DsaAttempts
            .AsNoTracking()
            .Where(attempt => attempt.DsaProblemId == dsaProblemId
                && attempt.Result == DsaAttemptResult.Solved)
            .OrderByDescending(attempt => attempt.AttemptNumber)
            .FirstOrDefaultAsync(cancellationToken);

    public async Task<IReadOnlyList<DsaAttempt>> GetByIdsAndProblemIdAsync(
        Guid dsaProblemId, IReadOnlyCollection<Guid> attemptIds,
        CancellationToken cancellationToken)
    {
        if (attemptIds.Count == 0)
        {
            return [];
        }

        return await dbContext.DsaAttempts
            .AsNoTracking()
            .Where(attempt => attempt.DsaProblemId == dsaProblemId
                && attemptIds.Contains(attempt.Id))
            .ToListAsync(cancellationToken);
    }

    public void Add(DsaAttempt attempt) =>
        dbContext.DsaAttempts.Add(attempt);

    public void AddPracticeSubmission(DsaPracticeSubmission submission) =>
        dbContext.DsaPracticeSubmissions.Add(submission);

    public async Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception)
            when (exception.InnerException is PostgresException
            {
                ConstraintName:
                    "ux_dsa_attempts_problem_attempt_number"
            })
        {
            throw new ConflictException(
                DsaAttemptErrors.VersionConflict.Code,
                DsaAttemptErrors.VersionConflict.Message);
        }
        catch (DbUpdateException exception)
            when (exception.InnerException is PostgresException
            {
                ConstraintName: "ux_dsa_practice_submissions_user_submission"
            })
        {
            throw new ConflictException(
                "DSA_PRACTICE_SUBMISSION_CONFLICT",
                "This practice submission was already recorded.");
        }
    }
}
