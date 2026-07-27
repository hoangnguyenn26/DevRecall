namespace DevRecall.Contracts.Interview;

public sealed class GetInterviewQuestionsRequest
{
    public string? Topic { get; init; }
    public string? Difficulty { get; init; }
    public int? Page { get; init; }
    public int? PageSize { get; init; }
}
