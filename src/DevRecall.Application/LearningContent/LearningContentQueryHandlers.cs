using DevRecall.Application.Common.Exceptions;
using DevRecall.Application.Identity;
using DevRecall.Domain.LearningContent;
using DevRecall.Domain.LearningProfiles;

namespace DevRecall.Application.LearningContent;

public sealed record GetPublishedLearningContentQuery(
    string? Technology, string? Topic, string? Difficulty, int Page, int PageSize);

public sealed record GetPublishedLearningContentResult(
    IReadOnlyList<PublishedLearningContentListItem> Items, int Page, int PageSize,
    int TotalCount, int TotalPages);

public sealed class GetPublishedLearningContentHandler(
    ILearningContentReader reader, ICurrentUser currentUser)
{
    public async Task<GetPublishedLearningContentResult> HandleAsync(
        GetPublishedLearningContentQuery query, CancellationToken cancellationToken)
    {
        EnsureAuthenticated(currentUser);
        var errors = new Dictionary<string, string[]>();
        if (query.Page < 1) errors["page"] = ["Page must be greater than or equal to 1."];
        if (query.PageSize is < 1 or > 50) errors["pageSize"] = ["Page size must be between 1 and 50."];
        var technology = ParseOptional<Technology>(query.Technology, "technology", errors);
        var difficulty = ParseOptional<ContentDifficulty>(query.Difficulty, "difficulty", errors);
        var topic = string.IsNullOrWhiteSpace(query.Topic) ? null : query.Topic.Trim().ToLowerInvariant();
        if (topic is not null && (topic.Length > 160 || topic.Any(character =>
            !(char.IsAsciiLetterOrDigit(character) || character == '-'))))
            errors["topic"] = ["Topic must be a valid lowercase slug."];
        if (errors.Count > 0) throw new ValidationException(errors);

        var page = await reader.GetPublishedAsync(technology?.ToString(), topic,
            difficulty?.ToString(), (query.Page - 1) * query.PageSize, query.PageSize,
            cancellationToken);
        var totalPages = page.TotalCount == 0 ? 0
            : (int)Math.Ceiling(page.TotalCount / (double)query.PageSize);
        return new(page.Items, query.Page, query.PageSize, page.TotalCount, totalPages);
    }

    internal static void EnsureAuthenticated(ICurrentUser user)
    {
        if (!user.IsAuthenticated || user.UserId is null)
            throw new UnauthorizedException("IDENTITY_UNAUTHENTICATED", "Authentication is required.");
    }

    private static T? ParseOptional<T>(string? value, string field,
        Dictionary<string, string[]> errors) where T : struct, Enum
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        if (Enum.TryParse<T>(value, false, out var parsed) && Enum.IsDefined(parsed)
            && !int.TryParse(value, out _)) return parsed;
        errors[field] = [$"{field} is not supported."];
        return null;
    }
}

public sealed class GetPublishedLearningContentDetailHandler(
    ILearningContentReader reader, ICurrentUser currentUser)
{
    public async Task<PublishedLearningContentDetail> HandleAsync(
        string slug, CancellationToken cancellationToken)
    {
        GetPublishedLearningContentHandler.EnsureAuthenticated(currentUser);
        if (string.IsNullOrWhiteSpace(slug)) throw new NotFoundException(
            "LEARNING_CONTENT_NOT_FOUND", "Learning content was not found.");
        return await reader.GetPublishedBySlugAsync(slug.Trim().ToLowerInvariant(), cancellationToken)
            ?? throw new NotFoundException("LEARNING_CONTENT_NOT_FOUND", "Learning content was not found.");
    }
}
