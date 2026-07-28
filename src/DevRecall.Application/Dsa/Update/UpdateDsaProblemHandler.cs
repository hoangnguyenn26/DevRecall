using DevRecall.Application.Common.Exceptions;
using DevRecall.Application.Identity;
using DevRecall.Domain.Dsa;

namespace DevRecall.Application.Dsa.Update;

public sealed class UpdateDsaProblemHandler(
    IDsaProblemRepository repository,
    ICurrentUser currentUser)
{
    public async Task<UpdateDsaProblemResult> HandleAsync(
        UpdateDsaProblemCommand command,
        CancellationToken cancellationToken)
    {
        DsaProblemInputValidator.Validate(
            command.Title, command.Description, command.Source,
            command.ExternalUrl, command.Topics);
        var difficulty = DsaProblemDifficultyParser.Parse(command.Difficulty);
        var userId = DsaHandlerSupport.GetCurrentUserId(currentUser);
        var problem = await repository.GetByIdAsync(
            command.Id, cancellationToken);
        if (problem is null || problem.UserId != userId)
        {
            throw new NotFoundException(
                DsaProblemErrors.NotFound.Code,
                DsaProblemErrors.NotFound.Message);
        }

        if (problem.Status == DsaProblemStatus.Archived)
        {
            throw new ConflictException(
                DsaProblemErrors.Archived.Code,
                DsaProblemErrors.Archived.Message);
        }

        var changed = problem.Update(
            command.Title, command.Description, difficulty, command.Source,
            command.ExternalUrl, command.Topics, DateTimeOffset.UtcNow);
        if (changed)
        {
            await repository.SaveChangesAsync(cancellationToken);
        }

        return MapResult(problem);
    }

    private static UpdateDsaProblemResult MapResult(DsaProblem problem) =>
        new(
            problem.Id, problem.Title, problem.Description,
            problem.Difficulty.ToString(), problem.Source, problem.ExternalUrl,
            problem.Topics.OrderBy(topic => topic.NormalizedName)
                .Select(topic => topic.Name).ToList(),
            problem.Status.ToString(), problem.CreatedAtUtc,
            problem.UpdatedAtUtc);
}
