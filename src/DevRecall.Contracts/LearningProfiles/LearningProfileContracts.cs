namespace DevRecall.Contracts.LearningProfiles;

public sealed record LearningProfileTechnologyResponse(string Name, bool IsPrimary);
public sealed record LearningProfileResponse(bool IsConfigured, string? TargetRole,
    string? ExperienceLevel, int? AvailableMinutesPerDay,
    IReadOnlyList<LearningProfileTechnologyResponse> Technologies,
    IReadOnlyList<string> Goals, int? Version);
public sealed record LearningProfileOptionResponse(string Value, string Label, string? Description);
public sealed record LearningProfileOptionsResponse(
    IReadOnlyList<LearningProfileOptionResponse> TargetRoles,
    IReadOnlyList<LearningProfileOptionResponse> ExperienceLevels,
    IReadOnlyList<LearningProfileOptionResponse> Technologies,
    IReadOnlyList<LearningProfileOptionResponse> Goals,
    IReadOnlyList<int> StudyTimeOptions);
public sealed record LearningProfileTechnologyRequest(string Name, bool IsPrimary);
public sealed record PutLearningProfileRequest(string TargetRole, string ExperienceLevel,
    int AvailableMinutesPerDay, IReadOnlyList<LearningProfileTechnologyRequest> Technologies,
    IReadOnlyList<string> Goals, int? ExpectedVersion);
