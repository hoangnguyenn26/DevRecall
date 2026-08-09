using DevRecall.Application.Common.Exceptions;
using DevRecall.Application.Identity;
using DevRecall.Domain.Dsa;

namespace DevRecall.Application.Dsa.Practice;

public sealed record GetDsaPracticeResult(
    Guid ProblemId, string Title, string? Description, string? ExternalUrl,
    string Difficulty, IReadOnlyList<string> Topics, int ProblemVersion);

public sealed class GetDsaPracticeHandler(
    IDsaProblemRepository problemRepository, ICurrentUser currentUser)
{
    public async Task<GetDsaPracticeResult> HandleAsync(Guid problemId, CancellationToken cancellationToken)
    {
        var userId = DsaHandlerSupport.GetCurrentUserId(currentUser);
        var problem = await problemRepository.GetByIdAndUserIdAsync(problemId, userId, cancellationToken);
        if (problem is null || problem.Status != DsaProblemStatus.Active)
            throw new NotFoundException(DsaProblemErrors.NotFound.Code, DsaProblemErrors.NotFound.Message);
        return new GetDsaPracticeResult(
            problem.Id, problem.Title, problem.Description, problem.ExternalUrl,
            problem.Difficulty.ToString(), problem.Topics.Select(item => item.Name).ToList(), 1);
    }
}
