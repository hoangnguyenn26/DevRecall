using DevRecall.Application.Interview.Answers;
using DevRecall.Domain.Interview.Answers;
using DevRecall.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

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

    public void Add(InterviewAnswerVersion answerVersion) =>
        dbContext.InterviewAnswerVersions.Add(answerVersion);

    public Task SaveChangesAsync(CancellationToken cancellationToken) =>
        dbContext.SaveChangesAsync(cancellationToken);
}
