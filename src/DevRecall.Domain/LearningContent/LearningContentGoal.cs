using DevRecall.Domain.LearningProfiles;

namespace DevRecall.Domain.LearningContent;

public sealed class LearningContentGoal
{
    private LearningContentGoal() { }
    internal LearningContentGoal(Guid learningContentId, LearningProfileGoal goal)
    {
        LearningContentId = learningContentId;
        Goal = goal;
    }
    public Guid LearningContentId { get; private set; }
    public LearningProfileGoal Goal { get; private set; }
}
