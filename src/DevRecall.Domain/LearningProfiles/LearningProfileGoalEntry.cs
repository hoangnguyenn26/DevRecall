namespace DevRecall.Domain.LearningProfiles;

public sealed class LearningProfileGoalEntry
{
    private LearningProfileGoalEntry() { }

    internal LearningProfileGoalEntry(Guid id, Guid learningProfileId, LearningProfileGoal goal)
    {
        Id = id;
        LearningProfileId = learningProfileId;
        Goal = goal;
    }

    public Guid Id { get; private set; }
    public Guid LearningProfileId { get; private set; }
    public LearningProfileGoal Goal { get; private set; }
}
