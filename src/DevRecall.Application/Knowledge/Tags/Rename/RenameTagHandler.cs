using DevRecall.Application.Common.Exceptions;
using DevRecall.Application.Identity;
using DevRecall.Domain.Knowledge.Tags;

namespace DevRecall.Application.Knowledge.Tags.Rename;

public sealed class RenameTagHandler(
    ITagRepository repository,
    ICurrentUser currentUser)
{
    public async Task<RenameTagResult> HandleAsync(
        RenameTagCommand command,
        CancellationToken cancellationToken)
    {
        TagHandlerSupport.ValidateName(command.Name);
        var userId = TagHandlerSupport.GetCurrentUserId(currentUser);
        var tag = await repository.GetByIdAsync(command.Id, cancellationToken);

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
                TagErrors.Archived.Message);
        }

        var normalizedName = TagName.NormalizeIdentity(command.Name);
        var duplicateExists = await repository.ExistsByNormalizedNameAsync(
            userId, normalizedName, tag.Id, cancellationToken);

        if (duplicateExists)
        {
            throw new ConflictException(
                TagErrors.NameAlreadyExists.Code,
                TagErrors.NameAlreadyExists.Message);
        }

        var changed = tag.Rename(command.Name, DateTimeOffset.UtcNow);
        if (changed)
        {
            await repository.SaveChangesAsync(cancellationToken);
        }

        return new RenameTagResult(
            tag.Id, tag.Name, tag.CreatedAtUtc, tag.UpdatedAtUtc);
    }
}
