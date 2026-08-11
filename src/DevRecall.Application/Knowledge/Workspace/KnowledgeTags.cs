using DevRecall.Application.Common.Exceptions;
using DevRecall.Application.Identity;
using DevRecall.Application.Knowledge.Tags;
using DevRecall.Domain.Knowledge.Tags;

namespace DevRecall.Application.Knowledge.Workspace;

public sealed record KnowledgeTagSummaryReadModel(
    Guid Id, string Name, string NormalizedName, int KnowledgeCount);

public interface IKnowledgeTagReader
{
    Task<IReadOnlyList<KnowledgeTagSummaryReadModel>> SearchAsync(
        Guid userId, string? query, int take, CancellationToken cancellationToken);
}

public sealed class GetKnowledgeTagsHandler(
    IKnowledgeTagReader reader, ICurrentUser currentUser)
{
    public Task<IReadOnlyList<KnowledgeTagSummaryReadModel>> HandleAsync(
        string? query, int take, CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        if (take is < 1 or > 20) throw new ValidationException(new Dictionary<string, string[]>
        { ["take"] = ["Take must be between 1 and 20."] });
        var normalizedQuery = string.IsNullOrWhiteSpace(query) ? null : query.Trim();
        return reader.SearchAsync(userId, normalizedQuery, take, cancellationToken);
    }

    private Guid GetUserId() => currentUser.IsAuthenticated && currentUser.UserId.HasValue
        ? currentUser.UserId.Value
        : throw new UnauthorizedException("IDENTITY_UNAUTHENTICATED", "Authentication is required.");
}

public sealed class CreateKnowledgeTagHandler(
    ITagRepository repository, ICurrentUser currentUser)
{
    public async Task<KnowledgeTagSummaryReadModel> HandleAsync(
        string name, CancellationToken cancellationToken)
    {
        var userId = currentUser.IsAuthenticated && currentUser.UserId.HasValue
            ? currentUser.UserId.Value
            : throw new UnauthorizedException("IDENTITY_UNAUTHENTICATED", "Authentication is required.");
        var normalizedName = TagName.NormalizeIdentity(name);
        var existing = await repository.GetByNormalizedNameAsync(userId, normalizedName, cancellationToken);
        if (existing is not null)
            return new KnowledgeTagSummaryReadModel(existing.Id, existing.Name, existing.NormalizedName, 0);

        var tag = Tag.Create(Guid.NewGuid(), userId, name, DateTimeOffset.UtcNow);
        repository.Add(tag);
        await repository.SaveChangesAsync(cancellationToken);
        return new KnowledgeTagSummaryReadModel(tag.Id, tag.Name, tag.NormalizedName, 0);
    }
}
