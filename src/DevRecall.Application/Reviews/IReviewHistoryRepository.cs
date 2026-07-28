using DevRecall.Domain.Reviews;

namespace DevRecall.Application.Reviews;

public interface IReviewHistoryRepository
{
    void Add(ReviewHistory history);
}
