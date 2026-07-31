using DevRecall.Application.Common.Exceptions;
using DevRecall.Application.Identity;

namespace DevRecall.Application.Search;

public sealed record GlobalSearchQuery(
    string? Text, IReadOnlyCollection<string>? Modules,
    int Page = 1, int PageSize = 20);
public sealed record GlobalSearchCandidate(
    string ResourceType, Guid ResourceId, string Title,
    string? Preview, double Rank,
    IReadOnlyDictionary<string, string> Metadata);
public sealed record GlobalSearchReadResult(
    IReadOnlyList<GlobalSearchCandidate> Items, int TotalCount);
public sealed record GlobalSearchResult(
    IReadOnlyList<GlobalSearchCandidate> Items, int Page, int PageSize,
    int TotalCount, int TotalPages);

public interface IGlobalSearchReader
{
    Task<GlobalSearchReadResult> SearchAsync(
        Guid userId, string text, IReadOnlySet<string> modules,
        int candidateLimit, CancellationToken cancellationToken);
}

public sealed class GlobalSearchHandler(
    IGlobalSearchReader reader, ICurrentUser currentUser)
{
    private static readonly HashSet<string> AllowedModules =
        new(StringComparer.OrdinalIgnoreCase) { "knowledge", "interview", "dsa" };

    public async Task<GlobalSearchResult> HandleAsync(
        GlobalSearchQuery query, CancellationToken cancellationToken)
    {
        var (text, modules) = Validate(query);
        var userId = currentUser.UserId
            ?? throw new UnauthorizedAccessException("An authenticated user is required.");
        var skip = (query.Page - 1) * query.PageSize;
        var readResult = await reader.SearchAsync(
            userId, text, modules, skip + query.PageSize, cancellationToken);
        var items = readResult.Items.Skip(skip).Take(query.PageSize).ToList();
        var totalPages = readResult.TotalCount == 0 ? 0
            : (int)Math.Ceiling(readResult.TotalCount / (double)query.PageSize);
        return new GlobalSearchResult(
            items, query.Page, query.PageSize, readResult.TotalCount, totalPages);
    }

    private static (string Text, IReadOnlySet<string> Modules) Validate(
        GlobalSearchQuery query)
    {
        var errors = new Dictionary<string, string[]>();
        var text = query.Text?.Trim() ?? string.Empty;
        if (text.Length is < 2 or > 100)
            errors["q"] = ["Search text must contain between 2 and 100 characters."];
        if (query.Page < 1)
            errors["page"] = ["Page must be greater than or equal to 1."];
        if (query.PageSize is < 1 or > 50)
            errors["pageSize"] = ["Page size must be between 1 and 50."];
        var modules = query.Modules is null || query.Modules.Count == 0
            ? new HashSet<string>(AllowedModules, StringComparer.OrdinalIgnoreCase)
            : new HashSet<string>(query.Modules, StringComparer.OrdinalIgnoreCase);
        var invalid = modules.Where(module => !AllowedModules.Contains(module)).ToArray();
        if (invalid.Length > 0)
            errors["modules"] = [$"Unsupported modules: {string.Join(", ", invalid)}."];
        if (errors.Count > 0) throw new ValidationException(errors);
        return (text, modules);
    }
}
