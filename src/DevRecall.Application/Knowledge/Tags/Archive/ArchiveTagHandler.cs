using DevRecall.Application.Common.Exceptions;
using DevRecall.Application.Identity;
using DevRecall.Domain.Knowledge.Tags;

namespace DevRecall.Application.Knowledge.Tags.Archive;

public sealed class ArchiveTagHandler(
    ITagRepository repository,
    ICurrentUser currentUser)
{
    public async Task HandleAsync(
        ArchiveTagCommand command,
        CancellationToken cancellationToken)
    {
        var userId = TagHandlerSupport.GetCurrentUserId(currentUser);
        var tag = await repository.GetByIdAsync(command.Id, cancellationToken);

        if (tag is null || tag.UserId != userId)
        {
            throw new NotFoundException(
                TagErrors.NotFound.Code,
                TagErrors.NotFound.Message);
        }

        if (tag.Archive(DateTimeOffset.UtcNow))
        {
            await repository.SaveChangesAsync(cancellationToken);
        }
    }
}
