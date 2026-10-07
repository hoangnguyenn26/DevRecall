using DevRecall.Application.Common.Exceptions;
using DevRecall.Application.Common.Time;
using DevRecall.Application.Identity;
using DevRecall.Domain.StudyPlans;

namespace DevRecall.Application.StudyPlans.LearningContent;

public sealed record AddLearningContentToStudyPlanCommand(Guid StudyPlanId, string Slug,
    int ExpectedVersion, Guid SubmissionId);
public sealed record LearningContentPlanSource(Guid Id, string Title, int EstimatedMinutes,
    bool IsCompleted);
public sealed record AddLearningContentToStudyPlanResult(Guid StudyPlanId, Guid ItemId,
    string PlanTitle, bool Added, int Version);
public sealed record LearningContentStudyPlanOption(Guid StudyPlanId, string Title, int ItemCount,
    int TotalPlannedDurationMinutes, int Version, bool AlreadyContains);

public interface ILearningContentPlanSourceReader
{
    Task<LearningContentPlanSource?> FindPublishedAsync(Guid userId, string slug,
        CancellationToken cancellationToken);
    Task<IReadOnlyList<LearningContentStudyPlanOption>> GetDraftOptionsAsync(Guid userId,
        string slug, CancellationToken cancellationToken);
}

public sealed class GetLearningContentStudyPlanOptionsHandler(ILearningContentPlanSourceReader reader,
    ICurrentUser currentUser)
{
    public async Task<IReadOnlyList<LearningContentStudyPlanOption>> HandleAsync(string slug,
        CancellationToken cancellationToken)
    {
        var userId = currentUser.UserId ?? throw new UnauthorizedException(
            "AUTH_REQUIRED", "Authentication is required.");
        if (string.IsNullOrWhiteSpace(slug)) throw new NotFoundException(
            "LEARNING_CONTENT_NOT_FOUND", "Learning content was not found.");
        return await reader.GetDraftOptionsAsync(userId, slug.Trim().ToLowerInvariant(), cancellationToken);
    }
}

public sealed class AddLearningContentToStudyPlanHandler(IStudyPlanRepository repository,
    ILearningContentPlanSourceReader sourceReader, ICurrentUser currentUser, IUtcClock clock)
{
    public async Task<AddLearningContentToStudyPlanResult> HandleAsync(
        AddLearningContentToStudyPlanCommand command, CancellationToken cancellationToken)
    {
        if (command.StudyPlanId == Guid.Empty || command.ExpectedVersion <= 0
            || command.SubmissionId == Guid.Empty || string.IsNullOrWhiteSpace(command.Slug))
            throw new ValidationException(new Dictionary<string, string[]>
            {
                ["request"] = ["Plan, learning content, expected version and submission id are required."]
            });
        var userId = currentUser.UserId ?? throw new UnauthorizedException(
            "AUTH_REQUIRED", "Authentication is required.");
        var plan = await repository.GetByIdAndUserIdForUpdateAsync(command.StudyPlanId, userId,
            cancellationToken) ?? throw new NotFoundException(StudyPlanErrors.NotFound.Code,
            StudyPlanErrors.NotFound.Message);
        var lesson = await sourceReader.FindPublishedAsync(userId, command.Slug.Trim().ToLowerInvariant(),
            cancellationToken) ?? throw new NotFoundException("LEARNING_CONTENT_NOT_FOUND",
            "Learning content was not found.");
        if (lesson.IsCompleted) throw new ConflictException("LEARNING_CONTENT_ALREADY_COMPLETED",
            "This lesson is already completed. Use Review or Read again to revisit it.");
        var count = plan.Items.Count;
        StudyPlanItem item;
        try
        {
            item = plan.EnsureManualItem(Guid.NewGuid(), StudyPlanResourceType.LearningContent,
                lesson.Id, lesson.EstimatedMinutes, command.ExpectedVersion, clock.UtcNow);
        }
        catch (StudyPlanDomainException exception)
        {
            throw new ConflictException(exception.Error.Code, exception.Error.Message);
        }
        var added = plan.Items.Count != count;
        if (added)
        {
            try { await repository.SaveChangesAsync(cancellationToken); }
            catch (DuplicateStudyPlanResourceException) { added = false; }
            catch (StudyPlanPersistenceConflictException)
            {
                throw new ConflictException(StudyPlanErrors.Conflict.Code, StudyPlanErrors.Conflict.Message);
            }
        }
        return new(plan.Id, item.Id, plan.Title, added, plan.Version);
    }
}
