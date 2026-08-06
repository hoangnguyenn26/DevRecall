using DevRecall.Application.Common.Exceptions;
using DevRecall.Application.Identity;

namespace DevRecall.Application.Search;

public enum GlobalSearchResourceType
{
    Knowledge = 1,
    InterviewQuestion = 2,
    DsaProblem = 3
}

public sealed record GlobalSearchQuery(string? Text, int TakePerType = 5);
public sealed record SearchHighlightReadModel(string Field, string Text);
public sealed record GlobalSearchCandidate(
    Guid ResourceId, GlobalSearchResourceType ResourceType, string Title,
    string? Summary, decimal Rank, DateTimeOffset UpdatedAtUtc,
    bool IsExactTitleMatch, bool IsPrefixTitleMatch);
public sealed record GlobalSearchReadModel(
    IReadOnlyList<GlobalSearchCandidate> Items, bool HasMore);
public sealed record GlobalSearchItem(
    Guid ResourceId, string ResourceType, string Title, string? Summary,
    string TargetPath, decimal Rank, DateTimeOffset UpdatedAtUtc,
    IReadOnlyList<SearchHighlightReadModel> Highlights);
public sealed record GlobalSearchResult(
    string Query, IReadOnlyList<GlobalSearchItem> Items, bool HasMore);

public interface IGlobalSearchReader
{
    Task<GlobalSearchReadModel> SearchAsync(
        Guid userId, string query, int takePerType,
        CancellationToken cancellationToken);
}

public sealed class GlobalSearchHandler(
    IGlobalSearchReader reader, ICurrentUser currentUser)
{
    public async Task<GlobalSearchResult> HandleAsync(
        GlobalSearchQuery query, CancellationToken cancellationToken)
    {
        var (text, takePerType) = Validate(query);
        var userId = currentUser.UserId
            ?? throw new UnauthorizedAccessException("An authenticated user is required.");
        var result = await reader.SearchAsync(
            userId, text, takePerType, cancellationToken);
        var items = result.Items.Select(item => new GlobalSearchItem(
            item.ResourceId, item.ResourceType.ToString(), item.Title,
            item.Summary, GetTargetPath(item.ResourceType, item.ResourceId),
            item.Rank, item.UpdatedAtUtc, [])).ToList();
        return new GlobalSearchResult(text, items, result.HasMore);
    }

    private static (string Text, int TakePerType) Validate(GlobalSearchQuery query)
    {
        var errors = new Dictionary<string, string[]>();
        var text = query.Text?.Trim() ?? string.Empty;
        if (text.Length is < 2 or > 200)
            errors["q"] = ["Search text must contain between 2 and 200 characters."];
        if (query.TakePerType is < 1 or > 10)
            errors["takePerType"] = ["Take per type must be between 1 and 10."];
        if (errors.Count > 0) throw new ValidationException(errors);
        return (text, query.TakePerType);
    }

    private static string GetTargetPath(
        GlobalSearchResourceType resourceType, Guid resourceId) =>
        resourceType switch
        {
            GlobalSearchResourceType.Knowledge => $"/app/knowledge/{resourceId}",
            GlobalSearchResourceType.InterviewQuestion => $"/app/interview/{resourceId}",
            GlobalSearchResourceType.DsaProblem => $"/app/dsa/{resourceId}",
            _ => throw new ArgumentOutOfRangeException(nameof(resourceType))
        };
}
