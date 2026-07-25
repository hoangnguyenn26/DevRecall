using DevRecall.Application.Common.Exceptions;
using DevRecall.Application.Identity;
using DevRecall.Application.Knowledge.Tags;
using DevRecall.Domain.Knowledge.Tags;

namespace DevRecall.Application.Knowledge.GetByTags;

public sealed class GetKnowledgeByTagsHandler(
    IKnowledgeNodeRepository knowledgeRepository,
    ITagRepository tagRepository,
    ICurrentUser currentUser)
{
    public async Task<IReadOnlyList<KnowledgeByTagsItem>> HandleAsync(
        GetKnowledgeByTagsQuery query,
        CancellationToken cancellationToken)
    {
        var userId = TagHandlerSupport.GetCurrentUserId(currentUser);
        var tagIds = ValidateAndNormalize(query.TagIds);
        var existingTagCount = await tagRepository.CountActiveByIdsAsync(
            userId,
            tagIds,
            cancellationToken);

        if (existingTagCount != tagIds.Length)
        {
            throw new NotFoundException(
                TagErrors.NotFound.Code,
                TagErrors.NotFound.Message);
        }

        var nodes = await knowledgeRepository.GetActiveByTagIdsAsync(
            userId,
            tagIds,
            cancellationToken);

        return nodes.Select(node => new KnowledgeByTagsItem(
                node.Id,
                node.ParentId,
                node.Title,
                node.Description,
                node.SortOrder,
                node.Tags))
            .ToList();
    }

    private static Guid[] ValidateAndNormalize(
        IReadOnlyCollection<Guid> tagIds)
    {
        var errors = new Dictionary<string, string[]>();

        if (tagIds.Count == 0)
        {
            errors["tagIds"] = ["At least one tag is required."];
        }
        else if (tagIds.Any(id => id == Guid.Empty))
        {
            errors["tagIds"] = ["Tag ids must be valid identifiers."];
        }

        if (errors.Count > 0)
        {
            throw new ValidationException(errors);
        }

        return tagIds.Distinct().ToArray();
    }
}
