using DevRecall.Domain.StudyPlans;

namespace DevRecall.Application.StudyPlans.Resources;

public sealed record StudyPlanResourceReference(
    StudyPlanResourceType ResourceType,
    Guid ResourceId);

public sealed record StudyPlanResourceSummary(
    StudyPlanResourceType ResourceType,
    Guid ResourceId,
    string Title,
    string? Preview,
    bool IsAvailable,
    string? PublicKey = null, string? ContentType = null, string? SourceName = null, string? ResourceKind = null);

public interface IStudyPlanResourceSummaryReader
{
    Task<IReadOnlyList<StudyPlanResourceSummary>> ReadManyAsync(
        Guid userId,
        IReadOnlyCollection<StudyPlanResourceReference> resources,
        CancellationToken cancellationToken);
}
