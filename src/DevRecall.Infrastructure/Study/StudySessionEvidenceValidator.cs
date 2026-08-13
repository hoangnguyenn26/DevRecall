using DevRecall.Application.Study.Items.Complete;
using DevRecall.Domain.Study;
using DevRecall.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace DevRecall.Infrastructure.Study;

internal sealed class StudySessionEvidenceValidator(DevRecallDbContext dbContext)
    : IStudySessionEvidenceValidator
{
    public Task<bool> IsValidAsync(
        Guid userId, StudyResourceType resourceType, Guid resourceId,
        Guid? evidenceId, DateTimeOffset? sessionStartedAtUtc, CancellationToken cancellationToken) =>
        resourceType == StudyResourceType.LearningContent && (evidenceId is null || sessionStartedAtUtc is null)
        ? Task.FromResult(false)
        : evidenceId is null ? Task.FromResult(true)
        : resourceType switch
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
        };
}
