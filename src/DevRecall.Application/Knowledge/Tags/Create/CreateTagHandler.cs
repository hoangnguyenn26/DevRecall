using DevRecall.Application.Common.Exceptions;
using DevRecall.Application.Identity;
using DevRecall.Domain.Knowledge.Tags;

namespace DevRecall.Application.Knowledge.Tags.Create;

public sealed class CreateTagHandler(
    ITagRepository repository,
    ICurrentUser currentUser)
{
    public async Task<CreateTagResult> HandleAsync(
        CreateTagCommand command,
        CancellationToken cancellationToken)
    {
        TagHandlerSupport.ValidateName(command.Name);
        var userId = TagHandlerSupport.GetCurrentUserId(currentUser);
        var normalizedName = TagName.NormalizeIdentity(command.Name);
        var alreadyExists = await repository.ExistsByNormalizedNameAsync(
            userId, normalizedName, excludedTagId: null, cancellationToken);

        if (alreadyExists)
        {
            throw new ConflictException(
                TagErrors.NameAlreadyExists.Code,
                TagErrors.NameAlreadyExists.Message);
        }

        var tag = Tag.Create(
            Guid.NewGuid(), userId, command.Name, DateTimeOffset.UtcNow);

        repository.Add(tag);
        await repository.SaveChangesAsync(cancellationToken);

        return new CreateTagResult(
            tag.Id, tag.Name, tag.CreatedAtUtc, tag.UpdatedAtUtc);
    }
}
