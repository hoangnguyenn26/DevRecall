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

    [Fact]
    public void CreateDraft_ShouldPreserveStableOrderedReviewCandidates()
    {
        var content = CreateDraft(reviewCandidates:
        [
            new("service-lifetimes", "What are the service lifetimes?", "Transient, Scoped, Singleton."),
            new("captive-dependency", "What is a captive dependency?", "A longer-lived service captures a shorter-lived one.")
        ]);

        content.ReviewCandidates.Select(item => (item.Position, item.Key)).Should().Equal(
            (0, "service-lifetimes"), (1, "captive-dependency"));
    }

    [Theory]
    [InlineData("CaptiveDependency")]
    [InlineData("captive_dependency")]
    [InlineData("-captive-dependency")]
    public void CreateDraft_ShouldRejectInvalidReviewCandidateKey(string key)
    {
        var action = () => CreateDraft(reviewCandidates: [new(key, "Prompt", "Answer")]);
        action.Should().Throw<ArgumentException>();
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

    [Fact]
    public void Goals_ShouldBeCanonicalUniqueBoundedAndIdempotent()
    {
        var content = CreateDraft();
        content.SetGoals([LearningProfileGoal.PrepareForInterviews, LearningProfileGoal.ImproveBackendFundamentals], Now);
        var version = content.Version;
        content.SetGoals([LearningProfileGoal.ImproveBackendFundamentals, LearningProfileGoal.PrepareForInterviews], Now);
        content.Version.Should().Be(version);
        content.Goals.Should().HaveCount(2);
        var duplicate = () => content.SetGoals([LearningProfileGoal.ImproveDsa, LearningProfileGoal.ImproveDsa], Now);
        duplicate.Should().Throw<ArgumentException>();
        var unknown = () => content.SetGoals([(LearningProfileGoal)99], Now);
        unknown.Should().Throw<ArgumentException>();
        var excessive = () => content.SetGoals(Enum.GetValues<LearningProfileGoal>().Take(4).ToArray(), Now);
        excessive.Should().Throw<ArgumentException>();
        content.Goals.Should().HaveCount(2);
    }

    [Fact]
    public void MetadataUpdates_ShouldBeValidatedIdempotentAndPreservePublishedContent()
    {
        var content = CreateDraft();
        content.Publish(Now);
        var id = content.Id;
        var published = content.PublishedAtUtc;
        var sectionIds = content.Sections.Select(item => item.Id).ToArray();
        var invalid = () => content.UpdateLearningMetadata([Technology.DotNet], ContentDifficulty.Beginner, 0, Now);
        invalid.Should().Throw<ArgumentOutOfRangeException>();
        content.Technologies.Should().ContainSingle(item => item.Technology == Technology.AspNetCore);
        content.UpdateLearningMetadata([Technology.DotNet], ContentDifficulty.Beginner, 10, Now.AddMinutes(1));
        var version = content.Version;
        content.UpdateLearningMetadata([Technology.DotNet], ContentDifficulty.Beginner, 10, Now.AddMinutes(2));
        content.Version.Should().Be(version);
        content.Id.Should().Be(id);
        content.Status.Should().Be(ContentStatus.Published);
        content.PublishedAtUtc.Should().Be(published);
        content.Sections.Select(item => item.Id).Should().Equal(sectionIds);
        var duplicate = () => content.UpdateLearningMetadata([Technology.DotNet, Technology.DotNet], content.Difficulty, 10, Now);
        duplicate.Should().Throw<ArgumentException>();
        // Generic content can remain technology-neutral.
        content.UpdateLearningMetadata([], content.Difficulty, 10, Now);
        content.Technologies.Should().BeEmpty();
    }

    private static LearningContentAggregate CreateDraft(int estimatedMinutes = 15,
        LearningContentType contentType = LearningContentType.Lesson,
        ContentSourceType sourceType = ContentSourceType.Internal, string? sourceUrl = null,
        IReadOnlyCollection<Technology>? technologies = null, IReadOnlyCollection<Guid>? topicIds = null,
        IReadOnlyCollection<LearningObjectiveInput>? objectives = null,
        IReadOnlyCollection<LearningContentSectionInput>? sections = null,
        IReadOnlyCollection<LearningReviewCandidateInput>? reviewCandidates = null) =>
        LearningContentAggregate.CreateDraft(Guid.NewGuid(), "aspnet-core-di-lifetimes",
            "ASP.NET Core DI Lifetimes", "Understand service lifetime trade-offs.",
            contentType, ContentDifficulty.Intermediate, estimatedMinutes, sourceType,
            sourceType == ContentSourceType.Internal ? "DevRecall" : "Microsoft Learn", sourceUrl,
            technologies ?? [Technology.AspNetCore], topicIds ?? [TopicId], objectives ??
            [new("Explain service lifetimes."), new("Choose an appropriate lifetime.")], sections ??
            [new(LearningContentSectionType.Explanation, "Why lifetime matters", "A lifetime controls reuse."),
             new(LearningContentSectionType.CodeExample, "Registration", "```csharp\nservices.AddScoped<IService, Service>();\n```")],
            Now, reviewCandidates);
}
