using DevRecall.Application.Common.Exceptions;
using DevRecall.Application.Identity;
using DevRecall.Domain.Dsa;

namespace DevRecall.Application.Dsa.Archive;

public sealed class ArchiveDsaProblemHandler(
    IDsaProblemRepository repository,
    ICurrentUser currentUser)
{
    public async Task HandleAsync(
        ArchiveDsaProblemCommand command,
        CancellationToken cancellationToken)
    {
        var userId = DsaHandlerSupport.GetCurrentUserId(currentUser);
        var problem = await repository.GetByIdAsync(
            command.Id, cancellationToken);
        if (problem is null || problem.UserId != userId)
        {
            throw new NotFoundException(
                DsaProblemErrors.NotFound.Code,
                DsaProblemErrors.NotFound.Message);
        }

        if (problem.Archive(DateTimeOffset.UtcNow))
        {
            await repository.SaveChangesAsync(cancellationToken);
        }
    }
}
