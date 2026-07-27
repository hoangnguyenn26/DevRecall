namespace DevRecall.Application.Interview.GetList;

public sealed record GetInterviewQuestionsQuery(
    string? Topic,
    string? Difficulty,
    int Page,
    int PageSize);
