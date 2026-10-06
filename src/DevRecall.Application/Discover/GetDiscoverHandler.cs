using DevRecall.Application.Common.Exceptions;
using DevRecall.Application.Identity;
using DevRecall.Application.LearningContent;
using DevRecall.Application.LearningProfiles;

namespace DevRecall.Application.Discover;

public sealed record DiscoverReason(string Type, LearningProfileValueResult? Goal = null,
    string? Value = null, string? Label = null, int? AvailableMinutes = null);
public sealed record DiscoverLesson(string Slug, string Title, string Summary, string Difficulty,
    int EstimatedMinutes, IReadOnlyList<LearningContentTechnologyItem> Technologies,
    IReadOnlyList<LearningContentTopicItem> Topics, IReadOnlyList<DiscoverReason> Reasons);
public sealed record DiscoverResult(bool ProfileConfigured, IReadOnlyList<DiscoverLesson> BasedOnGoals,
    IReadOnlyList<DiscoverLesson> BasedOnWeakTopics, IReadOnlyList<DiscoverLesson> Recommended);
public interface IDiscoverReader
{
    Task<DiscoverInputs> GetAsync(Guid userId, CancellationToken cancellationToken);
}
public sealed class GetDiscoverHandler(IDiscoverReader reader, ICurrentUser currentUser)
{
    public async Task<DiscoverResult> HandleAsync(CancellationToken cancellationToken)
    {
        if (!currentUser.IsAuthenticated || currentUser.UserId is null)
            throw new UnauthorizedException("IDENTITY_UNAUTHENTICATED", "Authentication is required.");
        return LearningRecommendationPolicy.Build(await reader.GetAsync(currentUser.UserId.Value, cancellationToken));
    }
}
