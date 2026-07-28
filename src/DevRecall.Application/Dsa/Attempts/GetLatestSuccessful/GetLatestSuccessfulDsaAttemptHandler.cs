using DevRecall.Application.Common.Exceptions;
using DevRecall.Application.Dsa.Attempts.GetDetail;
using DevRecall.Application.Identity;
using DevRecall.Domain.Dsa;

namespace DevRecall.Application.Dsa.Attempts.GetLatestSuccessful;

public sealed class GetLatestSuccessfulDsaAttemptHandler(
    IDsaProblemRepository problemRepository,
    IDsaAttemptRepository attemptRepository,
    ICurrentUser currentUser)
{
    public async Task<GetDsaAttemptDetailResult?> HandleAsync(
        GetLatestSuccessfulDsaAttemptQuery query,
        CancellationToken cancellationToken)
    {
        var userId = DsaHandlerSupport.GetCurrentUserId(currentUser);
        var problem = await problemRepository.GetByIdAndUserIdAsync(
            query.DsaProblemId, userId, cancellationToken);
        if (problem is null)
        {
            throw new NotFoundException(
                DsaProblemErrors.NotFound.Code,
                DsaProblemErrors.NotFound.Message);
        }

        var attempt = await attemptRepository.GetLatestSuccessfulAsync(
            problem.Id, cancellationToken);
        return attempt is null ? null : DsaAttemptDetailMapper.Map(attempt);
    }
}
