using DevRecall.Domain.LearningProfiles;

namespace DevRecall.Application.LearningProfiles;

public static class LearningProfileMetadata
{
    public static LearningProfileOption[] RoleOptions() => Options<TargetRole>(Role);
    public static LearningProfileOption[] LevelOptions() => Options<ExperienceLevel>(Level);
    public static LearningProfileOption[] GoalOptions() => Options<LearningProfileGoal>(Goal);
    public static LearningProfileTechnologyGroupResult[] TechnologyGroups() =>
    [
        Group("Languages", Technology.CSharp, Technology.Java, Technology.JavaScript, Technology.TypeScript),
        Group("Frameworks", Technology.DotNet, Technology.AspNetCore, Technology.EfCore,
            Technology.Spring, Technology.Vue, Technology.React),
        Group("Databases", Technology.Sql, Technology.PostgreSql, Technology.SqlServer),
        Group("Engineering", Technology.Docker, Technology.Git, Technology.SystemDesign,
            Technology.DataStructuresAlgorithms)
    ];
    public static IReadOnlyList<int> StudyTimeOptions => LearningProfile.StudyTimeOptions;

    public static LearningProfileValueResult RoleValue(TargetRole value) => new(value.ToString(), Role(value).Label);
    public static LearningProfileValueResult LevelValue(ExperienceLevel value) => new(value.ToString(), Level(value).Label);
    public static LearningProfileValueResult GoalValue(LearningProfileGoal value) => new(value.ToString(), Goal(value).Label);
    public static LearningProfileTechnologyResult TechnologyValue(Technology value, bool isPrimary) =>
        new(value.ToString(), TechnologyMetadata(value).Label, isPrimary);

    private static LearningProfileTechnologyGroupResult Group(string name, params Technology[] values) =>
        new(name, values.Select(value => Option(value, TechnologyMetadata)).ToArray());
    private static LearningProfileOption[] Options<T>(Func<T, (string Label, string? Description)> metadata)
        where T : struct, Enum => Enum.GetValues<T>().Select(value => Option(value, metadata)).ToArray();
    private static LearningProfileOption Option<T>(T value, Func<T, (string Label, string? Description)> metadata)
        where T : struct, Enum
    {
        var item = metadata(value);
        return new(value.ToString(), item.Label, item.Description);
    }

    private static (string Label, string? Description) Role(TargetRole value) => (value switch
    {
        TargetRole.BackendDeveloper => "Backend Developer",
        TargetRole.FrontendDeveloper => "Frontend Developer",
        TargetRole.FullStackDeveloper => "Full Stack Developer",
        TargetRole.MobileDeveloper => "Mobile Developer",
        TargetRole.DataEngineer => "Data Engineer",
        TargetRole.DevOpsEngineer => "DevOps Engineer",
        _ => "Other"
    }, null);
    private static (string Label, string? Description) Level(ExperienceLevel value) => value switch
    {
        ExperienceLevel.Beginner => ("Beginner", "I'm still learning the fundamentals."),
        ExperienceLevel.Junior => ("Junior", "I can build features with guidance."),
        ExperienceLevel.MidLevel => ("Mid-level", "I can independently deliver most features."),
        _ => ("Senior", "I regularly design solutions and guide others.")
    };
    private static (string Label, string? Description) TechnologyMetadata(Technology value) => (value switch
    {
        Technology.CSharp => "C#",
        Technology.DotNet => ".NET",
        Technology.AspNetCore => "ASP.NET Core",
        Technology.EfCore => "EF Core",
        Technology.PostgreSql => "PostgreSQL",
        Technology.SqlServer => "SQL Server",
        Technology.DataStructuresAlgorithms => "Data Structures & Algorithms",
        Technology.SystemDesign => "System Design",
        _ => value.ToString()
    }, null);
    private static (string Label, string? Description) Goal(LearningProfileGoal value) => value switch
    {
        LearningProfileGoal.PrepareForInterviews => ("Prepare for technical interviews",
            "Prioritize core concepts and common interview topics."),
        LearningProfileGoal.ImproveBackendFundamentals => ("Strengthen backend fundamentals",
            "APIs, databases, architecture and backend engineering basics."),
        LearningProfileGoal.ImproveDsa => ("Improve DSA", "Algorithms, data structures and problem-solving practice."),
        LearningProfileGoal.LearnNewTechnology => ("Learn a new technology", null),
        LearningProfileGoal.BuildProjects => ("Build practical projects", null),
        _ => ("Improve system design", "Design reliable systems and reason about trade-offs.")
    };
}
