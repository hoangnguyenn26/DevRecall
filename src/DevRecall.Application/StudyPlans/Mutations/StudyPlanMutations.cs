using DevRecall.Application.Common.Exceptions;
using DevRecall.Application.Common.Time;
using DevRecall.Application.Identity;
using DevRecall.Application.StudyPlans.Resources;
using DevRecall.Domain.Common.Errors;
using DevRecall.Domain.StudyPlans;

namespace DevRecall.Application.StudyPlans.Mutations;

public sealed record UpdateStudyPlanCommand(
    Guid StudyPlanId, string Title, int ExpectedVersion);
public sealed record ReplaceStudyPlanDraftItem(
    Guid? ItemId, string ResourceType, Guid ResourceId,
    int PlannedDurationMinutes);
public sealed record ReplaceStudyPlanDraftCommand(
    Guid StudyPlanId, string Title,
    IReadOnlyList<ReplaceStudyPlanDraftItem> Items, int ExpectedVersion);
public sealed record UpdateStudyPlanItemCommand(
    Guid StudyPlanId, Guid ItemId, int PlannedDurationMinutes,
    int ExpectedVersion);
public sealed record RemoveStudyPlanItemCommand(
    Guid StudyPlanId, Guid ItemId, int ExpectedVersion);
public sealed record ReorderStudyPlanItemsCommand(
    Guid StudyPlanId, IReadOnlyList<Guid> ItemIds, int ExpectedVersion);
public sealed record MarkStudyPlanReadyCommand(
    Guid StudyPlanId, int ExpectedVersion);
public sealed record CancelStudyPlanCommand(
    Guid StudyPlanId, int ExpectedVersion);

public sealed record StudyPlanMutationResult(
    Guid StudyPlanId, string Status, int ItemCount,
    int TotalPlannedDurationMinutes, DateTimeOffset UpdatedAtUtc,
    DateTimeOffset? ReadyAtUtc, DateTimeOffset? CancelledAtUtc, int Version);

public abstract class StudyPlanMutationHandler(
    IStudyPlanRepository repository,
    ICurrentUser currentUser,
    IUtcClock utcClock)
{
    protected IStudyPlanRepository Repository { get; } = repository;
    protected DateTimeOffset UtcNow => utcClock.UtcNow;
    protected Guid UserId => currentUser.UserId
        ?? throw new UnauthorizedException(
            "AUTH_REQUIRED", "Authentication is required.");

    protected async Task<StudyPlan> LoadAsync(
        Guid planId, int expectedVersion, CancellationToken cancellationToken)
    {
        var errors = new Dictionary<string, string[]>();
        if (planId == Guid.Empty)
        {
            errors["studyPlanId"] = ["Study plan id is required."];
        }

        if (expectedVersion <= 0)
        {
            errors["expectedVersion"] = ["Expected version must be positive."];
        }

        if (errors.Count > 0)
        {
            throw new ValidationException(errors);
        }

        var userId = currentUser.UserId
            ?? throw new UnauthorizedException(
                "AUTH_REQUIRED", "Authentication is required.");
        return await Repository.GetByIdAndUserIdForUpdateAsync(
            planId, userId, cancellationToken)
            ?? throw new NotFoundException(
                StudyPlanErrors.NotFound.Code, StudyPlanErrors.NotFound.Message);
    }

    protected async Task SaveIfChangedAsync(
        bool changed, CancellationToken cancellationToken)
    {
        if (!changed)
        {
            return;
        }

        try
        {
            await Repository.SaveChangesAsync(cancellationToken);
        }
        catch (StudyPlanPersistenceConflictException)
        {
            throw new ConflictException(
                StudyPlanErrors.Conflict.Code, StudyPlanErrors.Conflict.Message);
        }
    }

    protected static StudyPlanMutationResult Result(StudyPlan plan) =>
        new(
            plan.Id, plan.Status.ToString(), plan.Items.Count,
            plan.TotalPlannedDurationMinutes, plan.UpdatedAtUtc,
            plan.ReadyAtUtc, plan.CancelledAtUtc, plan.Version);

    protected static Exception Map(StudyPlanDomainException exception)
    {
        var error = exception.Error;
        if (ReferenceEquals(error, StudyPlanErrors.ItemNotFound))
        {
            return new NotFoundException(error.Code, error.Message);
        }

        if (IsValidation(error))
        {
            return new ValidationException(new Dictionary<string, string[]>
            {
                ["studyPlan"] = [error.Message]
            });
        }

        return new ConflictException(error.Code, error.Message);
    }

    private static bool IsValidation(DomainError error) =>
        ReferenceEquals(error, StudyPlanErrors.TitleRequired)
        || ReferenceEquals(error, StudyPlanErrors.TitleTooLong)
        || ReferenceEquals(error, StudyPlanErrors.InvalidItemDuration)
        || ReferenceEquals(error, StudyPlanErrors.InvalidItemOrder)
        || ReferenceEquals(error, StudyPlanErrors.DurationLimitExceeded)
        || ReferenceEquals(error, StudyPlanErrors.ItemLimitReached)
        || ReferenceEquals(error, StudyPlanErrors.DuplicateResource);
}

public sealed class UpdateStudyPlanHandler(
    IStudyPlanRepository repository, ICurrentUser currentUser, IUtcClock utcClock)
    : StudyPlanMutationHandler(repository, currentUser, utcClock)
{
    public async Task<StudyPlanMutationResult> HandleAsync(
        UpdateStudyPlanCommand command, CancellationToken cancellationToken)
    {
        var plan = await LoadAsync(
            command.StudyPlanId, command.ExpectedVersion, cancellationToken);
        bool changed;
        try
        {
            changed = plan.Rename(command.Title, command.ExpectedVersion, UtcNow);
        }
        catch (StudyPlanDomainException exception)
        {
            throw Map(exception);
        }

        await SaveIfChangedAsync(changed, cancellationToken);
        return Result(plan);
    }
}

public sealed class ReplaceStudyPlanDraftHandler(
    IStudyPlanRepository repository, ICurrentUser currentUser, IUtcClock utcClock,
    IStudyPlanResourceSummaryReader resourceReader)
    : StudyPlanMutationHandler(repository, currentUser, utcClock)
{
    public async Task<StudyPlanMutationResult> HandleAsync(
        ReplaceStudyPlanDraftCommand command, CancellationToken cancellationToken)
    {
        if (command.Items is null)
        {
            throw new ValidationException(new Dictionary<string, string[]>
            {
                ["items"] = ["Study plan items are required."]
            });
        }

        var plan = await LoadAsync(
            command.StudyPlanId, command.ExpectedVersion, cancellationToken);
        var items = command.Items.Select((item, index) => new DraftStudyPlanItem(
            item.ItemId ?? Guid.NewGuid(), ParseResourceType(item.ResourceType),
            item.ResourceId, item.PlannedDurationMinutes)).ToArray();
        var userId = UserId;
        var references = items.Select(item => new StudyPlanResourceReference(
            item.ResourceType, item.ResourceId)).Distinct().ToArray();
        var resources = await resourceReader.ReadManyAsync(
            userId, references, cancellationToken);
        var available = resources.Where(item => item.IsAvailable)
            .Select(item => (item.ResourceType, item.ResourceId)).ToHashSet();
        if (references.Any(item => !available.Contains(
            (item.ResourceType, item.ResourceId))))
        {
            throw new ValidationException(new Dictionary<string, string[]>
            {
                ["items"] = [StudyPlanErrors.ResourceUnavailable.Message]
            });
        }

        bool changed;
        try
        {
            changed = plan.ReplaceDraft(
                command.Title, items, command.ExpectedVersion, UtcNow);
        }
        catch (StudyPlanDomainException exception)
        {
            throw Map(exception);
        }

        await SaveIfChangedAsync(changed, cancellationToken);
        return Result(plan);
    }

    private static StudyPlanResourceType ParseResourceType(string value) =>
        value?.Trim() switch
        {
            "KnowledgeNode" or "Knowledge" => StudyPlanResourceType.KnowledgeNode,
            "InterviewQuestion" => StudyPlanResourceType.InterviewQuestion,
            "DsaProblem" => StudyPlanResourceType.DsaProblem,
            "LearningContent" => StudyPlanResourceType.LearningContent,
            _ => throw new ValidationException(new Dictionary<string, string[]>
            {
                ["resourceType"] = ["Resource type is invalid."]
            })
        };
}

public sealed class UpdateStudyPlanItemHandler(
    IStudyPlanRepository repository, ICurrentUser currentUser, IUtcClock utcClock)
    : StudyPlanMutationHandler(repository, currentUser, utcClock)
{
    public async Task<StudyPlanMutationResult> HandleAsync(
        UpdateStudyPlanItemCommand command,
        CancellationToken cancellationToken)
    {
        if (command.ItemId == Guid.Empty)
        {
            throw new ValidationException(new Dictionary<string, string[]>
            {
                ["itemId"] = ["Item id is required."]
            });
        }

        var plan = await LoadAsync(
            command.StudyPlanId, command.ExpectedVersion, cancellationToken);
        bool changed;
        try
        {
            changed = plan.UpdateItemDuration(
                command.ItemId, command.PlannedDurationMinutes,
                command.ExpectedVersion, UtcNow);
        }
        catch (StudyPlanDomainException exception)
        {
            throw Map(exception);
        }

        await SaveIfChangedAsync(changed, cancellationToken);
        return Result(plan);
    }
}

public sealed class RemoveStudyPlanItemHandler(
    IStudyPlanRepository repository, ICurrentUser currentUser, IUtcClock utcClock)
    : StudyPlanMutationHandler(repository, currentUser, utcClock)
{
    public async Task<StudyPlanMutationResult> HandleAsync(
        RemoveStudyPlanItemCommand command,
        CancellationToken cancellationToken)
    {
        if (command.ItemId == Guid.Empty)
        {
            throw new ValidationException(new Dictionary<string, string[]>
            {
                ["itemId"] = ["Item id is required."]
            });
        }

        var plan = await LoadAsync(
            command.StudyPlanId, command.ExpectedVersion, cancellationToken);
        bool changed;
        try
        {
            changed = plan.RemoveItem(
                command.ItemId, command.ExpectedVersion, UtcNow);
        }
        catch (StudyPlanDomainException exception)
        {
            throw Map(exception);
        }

        await SaveIfChangedAsync(changed, cancellationToken);
        return Result(plan);
    }
}

public sealed class ReorderStudyPlanItemsHandler(
    IStudyPlanRepository repository, ICurrentUser currentUser, IUtcClock utcClock)
    : StudyPlanMutationHandler(repository, currentUser, utcClock)
{
    public async Task<StudyPlanMutationResult> HandleAsync(
        ReorderStudyPlanItemsCommand command,
        CancellationToken cancellationToken)
    {
        if (command.ItemIds is null)
        {
            throw new ValidationException(new Dictionary<string, string[]>
            {
                ["itemIds"] = ["Item ids are required."]
            });
        }

        var plan = await LoadAsync(
            command.StudyPlanId, command.ExpectedVersion, cancellationToken);
        bool changed;
        try
        {
            changed = plan.ReorderItems(
                command.ItemIds, command.ExpectedVersion, UtcNow);
        }
        catch (StudyPlanDomainException exception)
        {
            throw Map(exception);
        }

        await SaveIfChangedAsync(changed, cancellationToken);
        return Result(plan);
    }
}

public sealed class MarkStudyPlanReadyHandler(
    IStudyPlanRepository repository, ICurrentUser currentUser, IUtcClock utcClock,
    IStudyPlanResourceSummaryReader resourceReader)
    : StudyPlanMutationHandler(repository, currentUser, utcClock)
{
    public async Task<StudyPlanMutationResult> HandleAsync(
        MarkStudyPlanReadyCommand command,
        CancellationToken cancellationToken)
    {
        var plan = await LoadAsync(
            command.StudyPlanId, command.ExpectedVersion, cancellationToken);
        var references = plan.Items.Select(item => new StudyPlanResourceReference(
            item.ResourceType, item.ResourceId)).ToArray();
        var resources = await resourceReader.ReadManyAsync(
            UserId, references, cancellationToken);
        var available = resources.Where(item => item.IsAvailable)
            .Select(item => (item.ResourceType, item.ResourceId)).ToHashSet();
        if (references.Any(item => !available.Contains(
            (item.ResourceType, item.ResourceId))))
        {
            throw new ValidationException(new Dictionary<string, string[]>
            {
                ["items"] = [StudyPlanErrors.ResourceUnavailable.Message]
            });
        }

        bool changed;
        try
        {
            changed = plan.MarkReady(command.ExpectedVersion, UtcNow);
        }
        catch (StudyPlanDomainException exception)
        {
            throw Map(exception);
        }

        await SaveIfChangedAsync(changed, cancellationToken);
        return Result(plan);
    }
}

public sealed class CancelStudyPlanHandler(
    IStudyPlanRepository repository, ICurrentUser currentUser, IUtcClock utcClock)
    : StudyPlanMutationHandler(repository, currentUser, utcClock)
{
    public async Task<StudyPlanMutationResult> HandleAsync(
        CancelStudyPlanCommand command, CancellationToken cancellationToken)
    {
        var plan = await LoadAsync(
            command.StudyPlanId, command.ExpectedVersion, cancellationToken);
        bool changed;
        try
        {
            changed = plan.Cancel(command.ExpectedVersion, UtcNow);
        }
        catch (StudyPlanDomainException exception)
        {
            throw Map(exception);
        }

        await SaveIfChangedAsync(changed, cancellationToken);
        return Result(plan);
    }
}
