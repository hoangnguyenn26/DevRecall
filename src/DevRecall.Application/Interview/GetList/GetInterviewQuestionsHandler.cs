using DevRecall.Application.Common.Exceptions;
using DevRecall.Application.Identity;

namespace DevRecall.Application.Interview.GetList;

public sealed class GetInterviewQuestionsHandler(
    IInterviewQuestionRepository repository,
    ICurrentUser currentUser)
{
    private const int MaximumPageSize = 100;

    public async Task<GetInterviewQuestionsResult> HandleAsync(
        GetInterviewQuestionsQuery query,
        CancellationToken cancellationToken)
    {
        Validate(query);
        var userId = InterviewHandlerSupport.GetCurrentUserId(currentUser);
        var difficulty = InterviewQuestionDifficultyParser.ParseOptional(
            query.Difficulty);
        var normalizedTopic = string.IsNullOrWhiteSpace(query.Topic)
            ? null
            : string.Join(
                ' ',
                query.Topic.Split(
                    (char[]?)null,
                    StringSplitOptions.RemoveEmptyEntries));
        var skip = (query.Page - 1) * query.PageSize;
        var result = await repository.GetActiveListAsync(
            userId, normalizedTopic, difficulty, skip, query.PageSize,
            cancellationToken);
        var totalPages = result.TotalCount == 0
            ? 0
            : (int)Math.Ceiling(result.TotalCount / (double)query.PageSize);

        return new GetInterviewQuestionsResult(
            result.Items.Select(question => new InterviewQuestionListItem(
                    question.Id, question.Title, question.Topic,
                    question.Difficulty.ToString(), question.UpdatedAtUtc))
                .ToList(),
            query.Page, query.PageSize, result.TotalCount, totalPages);
    }

    private static void Validate(GetInterviewQuestionsQuery query)
    {
        var errors = new Dictionary<string, string[]>();

        if (query.Page < 1)
        {
            errors["page"] = ["Page must be greater than or equal to 1."];
        }

        if (query.PageSize < 1 || query.PageSize > MaximumPageSize)
        {
            errors["pageSize"] =
                [$"Page size must be between 1 and {MaximumPageSize}."];
        }

        if (errors.Count > 0)
        {
            throw new ValidationException(errors);
        }
    }
}
