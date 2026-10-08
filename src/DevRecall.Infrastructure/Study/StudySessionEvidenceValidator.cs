using DevRecall.Application.Study.Items.Complete;
using DevRecall.Domain.Study;
using DevRecall.Domain.LearningContent;
using DevRecall.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace DevRecall.Infrastructure.Study;

internal sealed class StudySessionEvidenceValidator(DevRecallDbContext dbContext)
    : IStudySessionEvidenceValidator
{
    public async Task<bool> IsValidAsync(
        Guid userId, StudyResourceType resourceType, Guid resourceId,
        Guid? evidenceId, DateTimeOffset? sessionStartedAtUtc, CancellationToken cancellationToken)
    {
        if (resourceType == StudyResourceType.LearningContent)
        {
            var content = await dbContext.LearningContents.AsNoTracking().Where(item => item.Id == resourceId)
                .Select(item => new { item.ContentType, item.Status }).SingleOrDefaultAsync(cancellationToken);
            if (content is null) return false;
            if (content.ContentType == LearningContentType.ExternalResource)
                return content.Status is ContentStatus.Published or ContentStatus.Archived
                    && sessionStartedAtUtc is not null && evidenceId is null;
            if (evidenceId is null || sessionStartedAtUtc is null) return false;
        }
        if (evidenceId is null) return true;
        return await (resourceType switch
        {
            StudyResourceType.KnowledgeNode => Task.FromResult(false),
            StudyResourceType.InterviewQuestion when evidenceId is not null =>
                dbContext.InterviewPracticeAttempts.AsNoTracking().AnyAsync(
                    attempt => attempt.Id == evidenceId && attempt.UserId == userId
                        && attempt.QuestionId == resourceId,
                    cancellationToken),
            StudyResourceType.DsaProblem when evidenceId is not null =>
                dbContext.DsaPracticeSubmissions.AsNoTracking().AnyAsync(
                    attempt => attempt.DsaAttemptId == evidenceId
                        && attempt.UserId == userId
                        && attempt.DsaProblemId == resourceId,
                    cancellationToken),
            StudyResourceType.LearningContent when evidenceId is not null && sessionStartedAtUtc is not null =>
                dbContext.LearningContentCompletionEvidence.AsNoTracking().AnyAsync(
                    evidence => evidence.Id == evidenceId && evidence.UserId == userId
                        && evidence.LearningContentId == resourceId
                        && evidence.CompletedAtUtc >= sessionStartedAtUtc.Value,
                    cancellationToken),
            _ => Task.FromResult(false)
        });
    }
}
