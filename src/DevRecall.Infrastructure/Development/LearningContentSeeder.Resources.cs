using DevRecall.Domain.LearningContent;
using DevRecall.Domain.LearningProfiles;
using Content = DevRecall.Domain.LearningContent.LearningContent;

namespace DevRecall.Infrastructure.Development;

public sealed partial class LearningContentSeeder
{
    // Manually reviewed Microsoft Learn references. Descriptions are original curator metadata, not copied articles.
    private static IReadOnlyList<Content> ExternalResources(DateTimeOffset now) =>
    [
        CreateResource(201, "aspnet-core-middleware-documentation", "ASP.NET Core middleware",
            "Deepen the pipeline lesson with request delegates, short-circuiting and concrete middleware ordering guidance.",
            "https://learn.microsoft.com/en-us/aspnet/core/fundamentals/middleware/?view=aspnetcore-10.0",
            20, [Technology.DotNet, Technology.AspNetCore], Id(3), [LearningProfileGoal.ImproveBackendFundamentals, LearningProfileGoal.BuildProjects], now),
        CreateResource(202, "aspnet-core-dependency-injection-documentation", "Dependency injection in ASP.NET Core",
            "Use this after DI and service lifetimes to inspect framework registration, lifetime behavior and scoped-service guidance.",
            "https://learn.microsoft.com/en-us/aspnet/core/fundamentals/dependency-injection?view=aspnetcore-10.0",
            25, [Technology.DotNet, Technology.AspNetCore], Id(1), [LearningProfileGoal.ImproveBackendFundamentals, LearningProfileGoal.BuildProjects], now),
        CreateResource(203, "dotnet-cancellation-in-managed-threads", "Cancellation in Managed Threads",
            "Extend the CancellationToken lesson with the .NET cooperative cancellation model and practical observation patterns.",
            "https://learn.microsoft.com/en-us/dotnet/standard/threading/cancellation-in-managed-threads",
            20, [Technology.CSharp, Technology.DotNet], Id(4), [LearningProfileGoal.ImproveBackendFundamentals], now),
        CreateResource(204, "ef-core-tracking-documentation", "Tracking vs. No-Tracking Queries",
            "Check EF Core-specific tracking, identity resolution and projection behavior when choosing a read-only query strategy.",
            "https://learn.microsoft.com/en-us/ef/core/querying/tracking",
            15, [Technology.DotNet, Technology.EfCore], Id(2), [LearningProfileGoal.ImproveBackendFundamentals], now),
        CreateResource(205, "ef-core-handling-concurrency-conflicts", "Handling Concurrency Conflicts",
            "Follow the concurrency lesson with EF Core details on configuring tokens and deliberately handling conflicting updates.",
            "https://learn.microsoft.com/en-us/ef/core/saving/concurrency",
            20, [Technology.DotNet, Technology.EfCore], Id(6), [LearningProfileGoal.ImproveBackendFundamentals, LearningProfileGoal.PrepareForInterviews], now)
    ];

    private static Content CreateResource(int suffix, string slug, string title, string summary, string url,
        int minutes, IReadOnlyCollection<Technology> technologies, Guid topicId,
        IReadOnlyCollection<LearningProfileGoal> goals, DateTimeOffset now)
    {
        var resource = Content.CreateDraft(Id(suffix), slug, title, summary, LearningContentType.ExternalResource,
            ContentDifficulty.Intermediate, minutes, ContentSourceType.External, "Microsoft Learn", url,
            technologies, [topicId], [], [], now, resourceKind: ExternalResourceKind.Documentation);
        resource.SetGoals(goals, now);
        resource.Publish(now);
        return resource;
    }
}
