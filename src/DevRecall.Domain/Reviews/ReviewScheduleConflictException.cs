namespace DevRecall.Domain.Reviews;

public sealed class ReviewScheduleConflictException()
    : InvalidOperationException(ReviewErrors.ScheduleConflict.Message);
