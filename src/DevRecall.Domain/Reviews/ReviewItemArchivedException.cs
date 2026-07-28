namespace DevRecall.Domain.Reviews;

public sealed class ReviewItemArchivedException()
    : InvalidOperationException(ReviewErrors.ItemArchived.Message);
