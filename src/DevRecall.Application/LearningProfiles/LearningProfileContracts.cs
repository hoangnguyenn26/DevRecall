namespace DevRecall.Application.LearningProfiles;

public sealed record LearningProfileTechnologyResult(string Name, bool IsPrimary);
public sealed record LearningProfileResult(bool IsConfigured, string? TargetRole,
    string? ExperienceLevel, int? AvailableMinutesPerDay,
    IReadOnlyList<LearningProfileTechnologyResult> Technologies,
    IReadOnlyList<string> Goals, int? Version);
public sealed record LearningProfileOption(string Value, string Label, string? Description = null);
public sealed record LearningProfileOptionsResult(
    IReadOnlyList<LearningProfileOption> TargetRoles,
    IReadOnlyList<LearningProfileOption> ExperienceLevels,
    IReadOnlyList<LearningProfileOption> Technologies,
    IReadOnlyList<LearningProfileOption> Goals,
    IReadOnlyList<int> StudyTimeOptions);
public sealed record LearningProfileTechnologyInput(string Name, bool IsPrimary);
public sealed record PutLearningProfileCommand(string TargetRole, string ExperienceLevel,
    int AvailableMinutesPerDay, IReadOnlyList<LearningProfileTechnologyInput> Technologies,
    IReadOnlyList<string> Goals, int? ExpectedVersion);
