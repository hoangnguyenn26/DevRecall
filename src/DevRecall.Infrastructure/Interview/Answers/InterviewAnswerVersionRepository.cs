using DevRecall.Application.Interview.Answers;
using DevRecall.Domain.Interview.Answers;
using DevRecall.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace DevRecall.Infrastructure.Interview.Answers;

internal sealed class InterviewAnswerVersionRepository(
    DevRecallDbContext dbContext)
    : IInterviewAnswerVersionRepository
{
    public Task<InterviewAnswerVersion?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken) =>
        dbContext.InterviewAnswerVersions.SingleOrDefaultAsync(
            answer => answer.Id == id,
            cancellationToken);

    public Task<InterviewAnswerVersion?> GetByIdAndQuestionIdAsync(
        Guid id,
        Guid interviewQuestionId,
        CancellationToken cancellationToken) =>
        dbContext.InterviewAnswerVersions.SingleOrDefaultAsync(
            answer => answer.Id == id
                && answer.InterviewQuestionId == interviewQuestionId,
            cancellationToken);

    public Task<bool> HasDraftAsync(
        Guid interviewQuestionId,
        CancellationToken cancellationToken) =>
        dbContext.InterviewAnswerVersions
            .AsNoTracking()
            .AnyAsync(
                answer => answer.InterviewQuestionId == interviewQuestionId
                    && answer.Status == InterviewAnswerVersionStatus.Draft,
                cancellationToken);

    public async Task<int> GetNextVersionNumberAsync(
        Guid interviewQuestionId,
        CancellationToken cancellationToken)
    {
        var maximumVersionNumber = await dbContext.InterviewAnswerVersions
            .AsNoTracking()
            .Where(answer =>
                answer.InterviewQuestionId == interviewQuestionId)
            .Select(answer => (int?)answer.VersionNumber)
            .MaxAsync(cancellationToken);

        return maximumVersionNumber is null
            ? 1
            : maximumVersionNumber.Value + 1;
    }

    public Task<InterviewAnswerVersion?> GetCurrentPublishedAsync(
        Guid interviewQuestionId,
        CancellationToken cancellationToken) =>
        dbContext.InterviewAnswerVersions
            .AsNoTracking()
            .Where(answer =>
                answer.InterviewQuestionId == interviewQuestionId
                && answer.Status == InterviewAnswerVersionStatus.Published)
            .OrderByDescending(answer => answer.VersionNumber)
            .FirstOrDefaultAsync(cancellationToken);

    public async Task<IReadOnlyList<InterviewAnswerVersion>>
        GetByQuestionIdAsync(
            Guid interviewQuestionId,
            CancellationToken cancellationToken) =>
        await dbContext.InterviewAnswerVersions
            .AsNoTracking()
            .Where(answer =>
                answer.InterviewQuestionId == interviewQuestionId)
            .OrderByDescending(answer => answer.VersionNumber)
            .ToListAsync(cancellationToken);

    public void Add(InterviewAnswerVersion answerVersion) =>
        dbContext.InterviewAnswerVersions.Add(answerVersion);

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
                    "ux_interview_answer_versions_one_draft_per_question"
                    or "ux_interview_answer_versions_question_version"
            })
        {
            throw new InterviewAnswerVersionPersistenceConflictException(
                "The answer version changed concurrently.", exception);
        }
    }
}
