using DevRecall.Application.Common.Exceptions;
using DevRecall.Application.Identity;
using DevRecall.Domain.Dsa;

namespace DevRecall.Application.Dsa.GetDetail;

public sealed class GetDsaProblemDetailHandler(
    IDsaProblemRepository repository,
    ICurrentUser currentUser)
{
    public async Task<GetDsaProblemDetailResult> HandleAsync(
        GetDsaProblemDetailQuery query,
        CancellationToken cancellationToken)
    {
        var userId = DsaHandlerSupport.GetCurrentUserId(currentUser);
        var problem = await repository.GetByIdAndUserIdAsync(
            query.Id, userId, cancellationToken);
        if (problem is null)
        {
            throw new NotFoundException(
                DsaProblemErrors.NotFound.Code,
                DsaProblemErrors.NotFound.Message);
        }

        return new GetDsaProblemDetailResult(
            problem.Id, problem.Title, problem.Description,
            problem.Difficulty.ToString(), problem.Source, problem.ExternalUrl,
            problem.Topics.OrderBy(topic => topic.NormalizedName)
                .Select(topic => topic.Name).ToList(),
            problem.Status.ToString(), problem.CreatedAtUtc,
            problem.UpdatedAtUtc);
    }
}
