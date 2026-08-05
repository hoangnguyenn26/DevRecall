namespace DevRecall.Domain.Identity;

public sealed class UserLearningFocusArea
{
    private UserLearningFocusArea()
    {
    }

    internal UserLearningFocusArea(Guid userId, LearningFocusArea area)
    {
        UserId = userId;
        Area = area;
    }

    public Guid UserId { get; private set; }
    public LearningFocusArea Area { get; private set; }
}
