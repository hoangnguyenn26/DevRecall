using DevRecall.Domain.Study;

namespace DevRecall.Application.Study.Resources;

public enum StudyResourceAvailability
{
    Available = 1,
    Archived = 2
}

public sealed record StudyResourceResolution(
    StudyResourceType ResourceType, Guid ResourceId,
    string Title, string? Preview,
    StudyResourceAvailability Availability);

public interface IStudyResourceResolver
{
    Task<StudyResourceResolution?> ResolveAsync(
        Guid userId, StudyResourceType resourceType, Guid resourceId,
        CancellationToken cancellationToken);
}

public interface IStudyReviewItemResourceReader
{
    Task<StudyResourceResolution?> FindAsync(
        Guid userId, Guid resourceId, CancellationToken cancellationToken);
}
