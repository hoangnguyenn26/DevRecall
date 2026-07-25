using DevRecall.Application.Identity;

namespace DevRecall.Application.Knowledge.Tags.GetList;

public sealed class GetTagsHandler(
    ITagRepository repository,
    ICurrentUser currentUser)
{
    public async Task<IReadOnlyList<TagListItem>> HandleAsync(
        CancellationToken cancellationToken)
    {
        var userId = TagHandlerSupport.GetCurrentUserId(currentUser);
        var tags = await repository.GetActiveByUserIdAsync(
            userId,
            cancellationToken);

        return tags.Select(tag => new TagListItem(
                tag.Id, tag.Name, tag.CreatedAtUtc, tag.UpdatedAtUtc))
            .ToList();
    }
}
