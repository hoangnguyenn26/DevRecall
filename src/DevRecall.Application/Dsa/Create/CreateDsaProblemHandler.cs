using DevRecall.Application.Identity;
using DevRecall.Domain.Dsa;

namespace DevRecall.Application.Dsa.Create;

public sealed class CreateDsaProblemHandler(
    IDsaProblemRepository repository,
    ICurrentUser currentUser)
{
    public async Task<CreateDsaProblemResult> HandleAsync(
        CreateDsaProblemCommand command,
        CancellationToken cancellationToken)
    {
        DsaProblemInputValidator.Validate(
            command.Title, command.Description, command.Source,
            command.ExternalUrl, command.Topics);
        var difficulty = DsaProblemDifficultyParser.Parse(command.Difficulty);
        var userId = DsaHandlerSupport.GetCurrentUserId(currentUser);
        var now = DateTimeOffset.UtcNow;
        var problem = DsaProblem.Create(
            Guid.NewGuid(), userId, command.Title, command.Description,
            difficulty, command.Source, command.ExternalUrl, command.Topics,
            now);

        repository.Add(problem);
        await repository.SaveChangesAsync(cancellationToken);
        return MapResult(problem);
    }

    private static CreateDsaProblemResult MapResult(DsaProblem problem) =>
        new(
            problem.Id, problem.Title, problem.Description,
            problem.Difficulty.ToString(), problem.Source, problem.ExternalUrl,
            problem.Topics.OrderBy(topic => topic.NormalizedName)
                .Select(topic => topic.Name).ToList(),
            problem.Status.ToString(), problem.CreatedAtUtc,
            problem.UpdatedAtUtc);
}
