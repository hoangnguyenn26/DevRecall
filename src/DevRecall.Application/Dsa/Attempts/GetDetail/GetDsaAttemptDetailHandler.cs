using DevRecall.Application.Common.Exceptions;
using DevRecall.Application.Identity;
using DevRecall.Domain.Dsa;
using DevRecall.Domain.Dsa.Attempts;

namespace DevRecall.Application.Dsa.Attempts.GetDetail;

public sealed class GetDsaAttemptDetailHandler(
    IDsaProblemRepository problemRepository,
    IDsaAttemptRepository attemptRepository,
    ICurrentUser currentUser)
{
    public async Task<GetDsaAttemptDetailResult> HandleAsync(
        GetDsaAttemptDetailQuery query,
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

        var attempt = await attemptRepository.GetByIdAndProblemIdAsync(
            query.DsaAttemptId, problem.Id, cancellationToken);
        if (attempt is null)
        {
            throw new NotFoundException(
                DsaAttemptErrors.NotFound.Code,
                DsaAttemptErrors.NotFound.Message);
        }

        return DsaAttemptDetailMapper.Map(attempt);
    }
}
