namespace DevRecall.Contracts.LearningProfiles;

public sealed record LearningProfileValueResponse(string Value, string Label);
public sealed record LearningProfileTechnologyResponse(string Value, string Label, bool IsPrimary);
public sealed record LearningProfileResponse(bool IsConfigured,
    LearningProfileValueResponse? TargetRole, LearningProfileValueResponse? ExperienceLevel,
    int? AvailableMinutesPerDay, IReadOnlyList<LearningProfileTechnologyResponse> Technologies,
    IReadOnlyList<LearningProfileValueResponse> Goals, int? Version, DateTimeOffset? UpdatedAtUtc);
public sealed record LearningProfileOptionResponse(string Value, string Label, string? Description);
public sealed record LearningProfileTechnologyGroupResponse(string Name,
    IReadOnlyList<LearningProfileOptionResponse> Items);
public sealed record LearningProfileOptionsResponse(
    IReadOnlyList<LearningProfileOptionResponse> TargetRoles,
    IReadOnlyList<LearningProfileOptionResponse> ExperienceLevels,
    IReadOnlyList<LearningProfileTechnologyGroupResponse> TechnologyGroups,
    IReadOnlyList<LearningProfileOptionResponse> Goals,
    IReadOnlyList<int> StudyTimeOptions);
public sealed record LearningProfileTechnologyRequest(string Name, bool IsPrimary);
public sealed record PutLearningProfileRequest(string TargetRole, string ExperienceLevel,
    int AvailableMinutesPerDay, IReadOnlyList<LearningProfileTechnologyRequest> Technologies,
    IReadOnlyList<string> Goals, int? ExpectedVersion);
