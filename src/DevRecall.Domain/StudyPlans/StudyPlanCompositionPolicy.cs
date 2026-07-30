using DevRecall.Domain.Recommendations;

namespace DevRecall.Domain.StudyPlans;

public sealed record StudyPlanCompositionCandidate(
    Guid RecommendationId,
    RecommendationResourceType ResourceType,
    Guid ResourceId,
    RecommendationPriority Priority,
    decimal PriorityScore,
    DateTimeOffset GeneratedAtUtc);

public static class StudyPlanCompositionPolicy
{
    public static int GetDefaultDurationMinutes(RecommendationPriority priority) =>
        priority switch
        {
            RecommendationPriority.Low => 15,
            RecommendationPriority.Medium => 20,
            RecommendationPriority.High => 30,
            RecommendationPriority.Critical => 45,
            _ => throw new ArgumentOutOfRangeException(
                nameof(priority), priority, "Unsupported recommendation priority.")
        };

    public static IReadOnlyList<StudyPlanCompositionCandidate> Select(
        IEnumerable<StudyPlanCompositionCandidate> candidates,
        int totalDurationBudgetMinutes)
    {
        ArgumentNullException.ThrowIfNull(candidates);
        if (totalDurationBudgetMinutes
            is < StudyPlanDefaults.MinimumItemDurationMinutes
            or > StudyPlanDefaults.MaximumTotalDurationMinutes)
        {
            throw new ArgumentOutOfRangeException(
                nameof(totalDurationBudgetMinutes));
        }

        var selected = new List<StudyPlanCompositionCandidate>();
        var resources = new HashSet<(RecommendationResourceType, Guid)>();
        var usedMinutes = 0;
        foreach (var candidate in candidates
            .OrderByDescending(x => x.Priority)
            .ThenByDescending(x => x.PriorityScore)
            .ThenByDescending(x => x.GeneratedAtUtc)
            .ThenBy(x => x.RecommendationId))
        {
            if (selected.Count == StudyPlanDefaults.MaximumItems)
            {
                break;
            }

            if (!resources.Add((candidate.ResourceType, candidate.ResourceId)))
            {
                continue;
            }

            var duration = GetDefaultDurationMinutes(candidate.Priority);
            if (usedMinutes + duration > totalDurationBudgetMinutes)
            {
                continue;
            }

            selected.Add(candidate);
            usedMinutes += duration;
        }

        return selected;
    }
}
