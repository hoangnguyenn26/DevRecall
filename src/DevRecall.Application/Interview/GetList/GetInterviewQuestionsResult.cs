namespace DevRecall.Application.Interview.GetList;

public sealed record GetInterviewQuestionsResult(
    IReadOnlyList<InterviewQuestionListItem> Items,
    int Page,
    int PageSize,
    int TotalCount,
    int TotalPages);
