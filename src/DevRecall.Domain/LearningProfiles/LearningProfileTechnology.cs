namespace DevRecall.Domain.LearningProfiles;

public sealed class LearningProfileTechnology
{
    private LearningProfileTechnology() { }

    internal LearningProfileTechnology(Guid id, Guid learningProfileId, Technology technology, bool isPrimary)
    {
        Id = id;
        LearningProfileId = learningProfileId;
        Technology = technology;
        IsPrimary = isPrimary;
    }

    public Guid Id { get; private set; }
    public Guid LearningProfileId { get; private set; }
    public Technology Technology { get; private set; }
    public bool IsPrimary { get; private set; }
}
