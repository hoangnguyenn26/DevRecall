using DevRecall.Application.Common.Exceptions;
using DevRecall.Application.Identity;
using DevRecall.Domain.Knowledge;
using DevRecall.Domain.Knowledge.Tags;

namespace DevRecall.Application.Knowledge.Tags.Remove;

public sealed class RemoveTagFromKnowledgeNodeHandler(
    IKnowledgeNodeRepository knowledgeRepository,
    ITagRepository tagRepository,
    IKnowledgeNodeTagRepository relationRepository,
    ICurrentUser currentUser)
{
    public async Task HandleAsync(
        RemoveTagFromKnowledgeNodeCommand command,
        CancellationToken cancellationToken)
    {
        var userId = TagHandlerSupport.GetCurrentUserId(currentUser);
        var node = await knowledgeRepository.GetByIdAsync(
            command.KnowledgeNodeId,
            cancellationToken);

        if (node is null || node.UserId != userId)
        {
            throw new NotFoundException(
                KnowledgeErrors.NodeNotFound.Code,
                KnowledgeErrors.NodeNotFound.Message);
        }

        if (node.Status == KnowledgeNodeStatus.Archived)
        {
            throw new ConflictException(
                "KNOWLEDGE_NODE_ARCHIVED",
                "An archived knowledge node cannot be modified.");
        }

        var tag = await tagRepository.GetByIdAsync(
            command.TagId,
            cancellationToken);

        if (tag is null || tag.UserId != userId)
        {
            throw new NotFoundException(
                TagErrors.NotFound.Code,
                TagErrors.NotFound.Message);
        }

        var relation = await relationRepository.GetAsync(
            node.Id, tag.Id, cancellationToken);

        if (relation is null)
        {
            return;
        }

        relationRepository.Remove(relation);
        await relationRepository.SaveChangesAsync(cancellationToken);
    }
}
