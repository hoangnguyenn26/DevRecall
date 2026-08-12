using DevRecall.Domain.LearningContent;
using DevRecall.Domain.LearningProfiles;
using FluentAssertions;
using LearningContentAggregate = DevRecall.Domain.LearningContent.LearningContent;

namespace DevRecall.Domain.Tests.LearningContent;

public sealed class LearningContentTests
{
    private static readonly DateTimeOffset Now = new(2026, 8, 12, 2, 0, 0, TimeSpan.Zero);
    private static readonly Guid TopicId = Guid.Parse("20000000-0000-0000-0000-000000000001");

    [Fact]
    public void CreateDraft_ShouldNormalizePositionsAndPreserveMarkdown()
    {
        var content = CreateDraft();

        content.Status.Should().Be(ContentStatus.Draft);
        content.Version.Should().Be(1);
        content.Objectives.Select(item => item.Position).Should().Equal(0, 1);
        content.Sections.Select(item => item.Position).Should().Equal(0, 1);
        content.Sections.ElementAt(1).BodyMarkdown.Should().Contain("```csharp");
    }

    [Fact]
    public void CreateDraft_ShouldRejectDuplicateTechnologiesAndTopics()
    {
        var duplicateTechnologies = () => CreateDraft(
            technologies: [Technology.AspNetCore, Technology.AspNetCore]);
        var duplicateTopics = () => CreateDraft(topicIds: [TopicId, TopicId]);

        duplicateTechnologies.Should().Throw<ArgumentException>();
        duplicateTopics.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(481)]
    public void CreateDraft_ShouldRejectInvalidEstimatedMinutes(int minutes)
    {
        var action = () => CreateDraft(estimatedMinutes: minutes);
        action.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void PublishLesson_ShouldRequireTopicObjectiveAndSection()
    {
        var withoutTopic = CreateDraft(topicIds: []);
        var withoutObjective = CreateDraft(objectives: []);
        var withoutSection = CreateDraft(sections: []);
        var publishWithoutTopic = () => withoutTopic.Publish(Now.AddMinutes(1));
        var publishWithoutObjective = () => withoutObjective.Publish(Now.AddMinutes(1));
        var publishWithoutSection = () => withoutSection.Publish(Now.AddMinutes(1));

        publishWithoutTopic.Should().Throw<InvalidOperationException>();
        publishWithoutObjective.Should().Throw<InvalidOperationException>();
        publishWithoutSection.Should().Throw<InvalidOperationException>();
    }

    [Theory]
    [InlineData("javascript:alert(1)")]
    [InlineData("file:///tmp/lesson.md")]
    [InlineData(null)]
    public void ExternalResource_ShouldRequireSafeHttpUrl(string? url)
    {
        var action = () => CreateDraft(contentType: LearningContentType.ExternalResource,
            sourceType: ContentSourceType.External, sourceUrl: url, objectives: [], sections: []);
        action.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void ArchiveAndRepublish_ShouldKeepFirstPublishedTimestamp()
    {
        var content = CreateDraft();
        content.Publish(Now.AddMinutes(1));
        var firstPublishedAt = content.PublishedAtUtc;
        content.Archive(Now.AddMinutes(2));
        content.Publish(Now.AddMinutes(3));

        content.Status.Should().Be(ContentStatus.Published);
        content.PublishedAtUtc.Should().Be(firstPublishedAt);
        content.Version.Should().Be(4);
    }

    [Fact]
    public void InvalidLifecycleTransitions_ShouldFail()
    {
        var content = CreateDraft();
        var archiveDraft = () => content.Archive(Now.AddMinutes(1));
        archiveDraft.Should().Throw<InvalidOperationException>();
        content.Publish(Now.AddMinutes(1));
        var publishAgain = () => content.Publish(Now.AddMinutes(2));
        publishAgain.Should().Throw<InvalidOperationException>();
    }

    private static LearningContentAggregate CreateDraft(int estimatedMinutes = 15,
        LearningContentType contentType = LearningContentType.Lesson,
        ContentSourceType sourceType = ContentSourceType.Internal, string? sourceUrl = null,
        IReadOnlyCollection<Technology>? technologies = null, IReadOnlyCollection<Guid>? topicIds = null,
        IReadOnlyCollection<LearningObjectiveInput>? objectives = null,
        IReadOnlyCollection<LearningContentSectionInput>? sections = null) =>
        LearningContentAggregate.CreateDraft(Guid.NewGuid(), "aspnet-core-di-lifetimes",
            "ASP.NET Core DI Lifetimes", "Understand service lifetime trade-offs.",
            contentType, ContentDifficulty.Intermediate, estimatedMinutes, sourceType,
            sourceType == ContentSourceType.Internal ? "DevRecall" : "Microsoft Learn", sourceUrl,
            technologies ?? [Technology.AspNetCore], topicIds ?? [TopicId], objectives ??
            [new("Explain service lifetimes."), new("Choose an appropriate lifetime.")], sections ??
            [new(LearningContentSectionType.Explanation, "Why lifetime matters", "A lifetime controls reuse."),
             new(LearningContentSectionType.CodeExample, "Registration", "```csharp\nservices.AddScoped<IService, Service>();\n```")], Now);
}
