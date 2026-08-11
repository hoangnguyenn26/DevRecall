namespace DevRecall.Application.LearningProfiles;

public sealed record LearningProfileValueResult(string Value, string Label);
public sealed record LearningProfileTechnologyResult(string Value, string Label, bool IsPrimary);
public sealed record LearningProfileResult(bool IsConfigured,
    LearningProfileValueResult? TargetRole, LearningProfileValueResult? ExperienceLevel,
    int? AvailableMinutesPerDay, IReadOnlyList<LearningProfileTechnologyResult> Technologies,
    IReadOnlyList<LearningProfileValueResult> Goals, int? Version, DateTimeOffset? UpdatedAtUtc);
public sealed record LearningProfileOption(string Value, string Label, string? Description = null);
public sealed record LearningProfileTechnologyGroupResult(string Name,
    IReadOnlyList<LearningProfileOption> Items);
public sealed record LearningProfileOptionsResult(
    IReadOnlyList<LearningProfileOption> TargetRoles,
    IReadOnlyList<LearningProfileOption> ExperienceLevels,
    IReadOnlyList<LearningProfileTechnologyGroupResult> TechnologyGroups,
    IReadOnlyList<LearningProfileOption> Goals,
    IReadOnlyList<int> StudyTimeOptions);
public sealed record LearningProfileTechnologyInput(string Name, bool IsPrimary);
public sealed record PutLearningProfileCommand(string TargetRole, string ExperienceLevel,
    int AvailableMinutesPerDay, IReadOnlyList<LearningProfileTechnologyInput> Technologies,
    IReadOnlyList<string> Goals, int? ExpectedVersion);
