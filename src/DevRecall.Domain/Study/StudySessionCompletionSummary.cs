namespace DevRecall.Domain.Study;

public sealed record StudySessionCompletionSummary(
    int TotalItems, int PendingItems, int InProgressItems,
    int CompletedItems, int SkippedItems,
    int KnowledgeItemsCompleted, int InterviewItemsCompleted,
    int DsaItemsCompleted, int ReviewItemsCompleted);
