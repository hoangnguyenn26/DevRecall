using DevRecall.Application.Common.Exceptions;
using DevRecall.Application.Identity;
using DevRecall.Domain.Knowledge;
using DevRecall.Domain.Knowledge.Tags;

namespace DevRecall.Application.Knowledge.Tags.Assign;

public sealed class AssignTagToKnowledgeNodeHandler(
    IKnowledgeNodeRepository knowledgeRepository,
    ITagRepository tagRepository,
    IKnowledgeNodeTagRepository relationRepository,
    ICurrentUser currentUser)
{
    public async Task HandleAsync(
        AssignTagToKnowledgeNodeCommand command,
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

        if (tag.Status == TagStatus.Archived)
        {
            throw new ConflictException(
                TagErrors.Archived.Code,
                "An archived tag cannot be assigned.");
        }

        if (await relationRepository.ExistsAsync(
            node.Id, tag.Id, cancellationToken))
        {
            return;
        }

        relationRepository.Add(KnowledgeNodeTag.Create(
            node.Id, tag.Id, DateTimeOffset.UtcNow));
        await relationRepository.SaveChangesAsync(cancellationToken);
    }
}
