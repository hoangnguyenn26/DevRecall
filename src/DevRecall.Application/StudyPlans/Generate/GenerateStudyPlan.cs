using DevRecall.Application.Common.Exceptions;
using DevRecall.Application.Common.Time;
using DevRecall.Application.Identity;
using DevRecall.Application.StudyPlans.Generation;
using DevRecall.Application.StudyPlans.Resources;
using DevRecall.Domain.StudyPlans;

namespace DevRecall.Application.StudyPlans.Generate;

public sealed record GenerateStudyPlanCommand(
    string Title, int TotalDurationMinutes, int MaximumCandidates);

public sealed record GenerateStudyPlanItemResult(
    Guid ItemId, Guid? SourceRecommendationId, string SourceType,
    string ResourceType, Guid ResourceId, string ResourceTitle,
    string? ResourcePreview, bool IsResourceAvailable,
    int PlannedDurationMinutes, int Position);

public sealed record GenerateStudyPlanResult(
    Guid StudyPlanId, string Title, string Status,
    int TotalPlannedDurationMinutes, int ItemCount,
    DateTimeOffset GeneratedAtUtc, DateTimeOffset? ExpiresAtUtc,
    int Version, IReadOnlyList<GenerateStudyPlanItemResult> Items);

public sealed class GenerateStudyPlanHandler(
    IStudyPlanRepository studyPlanRepository,
    IStudyPlanRecommendationCandidateReader candidateReader,
    IStudyPlanResourceSummaryReader resourceSummaryReader,
    ICurrentUser currentUser,
    IUtcClock utcClock)
{
    private const int MaximumCandidateLimit = 100;

    public async Task<GenerateStudyPlanResult> HandleAsync(
        GenerateStudyPlanCommand command,
        CancellationToken cancellationToken)
    {
        Validate(command);
        var userId = currentUser.UserId
            ?? throw new UnauthorizedException(
                "AUTH_REQUIRED", "Authentication is required.");
        var generatedAtUtc = utcClock.UtcNow;
        var existingDraft =
            await studyPlanRepository.GetDraftByUserIdForUpdateAsync(
                userId, cancellationToken);
        if (existingDraft is not null)
        {
            throw new ConflictException(
                StudyPlanErrors.DraftAlreadyExists.Code,
                StudyPlanErrors.DraftAlreadyExists.Message);
        }

        var candidates = await candidateReader.ReadAsync(
            userId, command.MaximumCandidates, generatedAtUtc,
            cancellationToken);
        var references = candidates.Select(candidate =>
            new StudyPlanResourceReference(
                StudyPlanMappingPolicy.MapResourceType(candidate.ResourceType),
                candidate.ResourceId)).Distinct().ToArray();
        var summaries = await resourceSummaryReader.ReadManyAsync(
            userId, references, cancellationToken);
        var resourcesByKey = summaries
            .GroupBy(item => (item.ResourceType, item.ResourceId))
            .ToDictionary(group => group.Key, group => group.First());
        var selectedItems = SelectItems(
            candidates, resourcesByKey, command.TotalDurationMinutes);
        if (selectedItems.Count == 0)
        {
            throw new UnprocessableEntityException(
                StudyPlanErrors.NoEligibleItems.Code,
                StudyPlanErrors.NoEligibleItems.Message);
        }

        var initialItems = selectedItems.Select(item =>
            new InitialStudyPlanItem(
                Guid.NewGuid(), item.RecommendationId, item.ResourceType,
                item.ResourceId, item.PlannedDurationMinutes)).ToArray();
        var plan = StudyPlan.CreateFromRecommendations(
            Guid.NewGuid(), userId, command.Title, initialItems, generatedAtUtc,
            generatedAtUtc.Add(StudyPlanDefaults.DefaultLifetime));
        studyPlanRepository.Add(plan);
        await SaveAsync(cancellationToken);
        return MapResult(plan, resourcesByKey);
    }

    private async Task SaveAsync(CancellationToken cancellationToken)
    {
        try
        {
            await studyPlanRepository.SaveChangesAsync(cancellationToken);
        }
        catch (DraftStudyPlanAlreadyExistsException)
        {
            throw new ConflictException(
                StudyPlanErrors.DraftAlreadyExists.Code,
                StudyPlanErrors.DraftAlreadyExists.Message);
        }
        catch (DuplicateStudyPlanResourceException)
        {
            throw new ConflictException(
                StudyPlanErrors.DuplicateResource.Code,
                StudyPlanErrors.DuplicateResource.Message);
        }
        catch (StudyPlanPersistenceConflictException)
        {
            throw new ConflictException(
                StudyPlanErrors.Conflict.Code,
                StudyPlanErrors.Conflict.Message);
        }
    }

    private static List<SelectedStudyPlanItem> SelectItems(
        IReadOnlyList<StudyPlanRecommendationCandidate> candidates,
        Dictionary<
            (StudyPlanResourceType ResourceType, Guid ResourceId),
            StudyPlanResourceSummary> resources,
        int totalDurationBudget)
    {
        var selected = new List<SelectedStudyPlanItem>();
        var selectedResources =
            new HashSet<(StudyPlanResourceType ResourceType, Guid ResourceId)>();
        var usedMinutes = 0;
        foreach (var candidate in candidates)
        {
            var resourceType =
                StudyPlanMappingPolicy.MapResourceType(candidate.ResourceType);
            var resourceKey = (resourceType, candidate.ResourceId);
            if (!resources.TryGetValue(resourceKey, out var resource)
                || !resource.IsAvailable
                || !selectedResources.Add(resourceKey))
            {
                continue;
            }

            var duration = StudyPlanCompositionPolicy
                .GetDefaultDurationMinutes(candidate.Priority);
            if (usedMinutes + duration > totalDurationBudget)
            {
                continue;
            }

            selected.Add(new(
                candidate.RecommendationId, resourceType, candidate.ResourceId,
                duration, resource));
            usedMinutes += duration;
            if (selected.Count >= StudyPlanDefaults.MaximumItems)
            {
                break;
            }
        }

        return selected;
    }

    private static GenerateStudyPlanResult MapResult(
        StudyPlan plan,
        Dictionary<
            (StudyPlanResourceType ResourceType, Guid ResourceId),
            StudyPlanResourceSummary> resources)
    {
        var items = plan.Items.OrderBy(item => item.Position).Select(item =>
        {
            var resource = resources[(item.ResourceType, item.ResourceId)];
            return new GenerateStudyPlanItemResult(
                item.Id, item.SourceRecommendationId,
                item.SourceType.ToString(), item.ResourceType.ToString(),
                item.ResourceId, resource.Title, resource.Preview,
                resource.IsAvailable, item.PlannedDurationMinutes,
                item.Position);
        }).ToArray();
        return new(
            plan.Id, plan.Title, plan.Status.ToString(),
            plan.TotalPlannedDurationMinutes, items.Length,
            plan.GeneratedAtUtc, plan.ExpiresAtUtc, plan.Version, items);
    }

    private static void Validate(GenerateStudyPlanCommand command)
    {
        var errors = new Dictionary<string, string[]>();
        if (string.IsNullOrWhiteSpace(command.Title))
        {
            errors["title"] = ["Title is required."];
        }
        else if (command.Title.Trim().Length > StudyPlanDefaults.MaximumTitleLength)
        {
            errors["title"] =
            [$"Title must not exceed {StudyPlanDefaults.MaximumTitleLength} characters."];
        }

        if (command.TotalDurationMinutes
            is < StudyPlanDefaults.MinimumItemDurationMinutes
            or > StudyPlanDefaults.MaximumTotalDurationMinutes)
        {
            errors["totalDurationMinutes"] =
            [
                $"Total duration must be between "
                + $"{StudyPlanDefaults.MinimumItemDurationMinutes} and "
                + $"{StudyPlanDefaults.MaximumTotalDurationMinutes} minutes."
            ];
        }

        if (command.MaximumCandidates is < 1 or > MaximumCandidateLimit)
        {
            errors["maximumCandidates"] =
            [$"Maximum candidates must be between 1 and {MaximumCandidateLimit}."];
        }

        if (errors.Count > 0)
        {
            throw new ValidationException(errors);
        }
    }

    private sealed record SelectedStudyPlanItem(
        Guid RecommendationId, StudyPlanResourceType ResourceType,
        Guid ResourceId, int PlannedDurationMinutes,
        StudyPlanResourceSummary Resource);
}
