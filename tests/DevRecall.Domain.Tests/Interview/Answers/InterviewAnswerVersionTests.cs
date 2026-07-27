using DevRecall.Domain.Interview.Answers;
using FluentAssertions;

namespace DevRecall.Domain.Tests.Interview.Answers;

public sealed class InterviewAnswerVersionTests
{
    [Fact]
    public void CreateDraft_ShouldCreateNormalizedDraftVersion()
    {
        var createdAtUtc = DateTimeOffset.UtcNow;

        var answer = InterviewAnswerVersion.CreateDraft(
            Guid.NewGuid(), Guid.NewGuid(), 1,
            "  IQueryable builds an expression tree.  ", createdAtUtc);

        answer.VersionNumber.Should().Be(1);
        answer.Content.Should().Be("IQueryable builds an expression tree.");
        answer.Status.Should().Be(InterviewAnswerVersionStatus.Draft);
        answer.CreatedAtUtc.Should().Be(createdAtUtc);
        answer.UpdatedAtUtc.Should().Be(createdAtUtc);
        answer.PublishedAtUtc.Should().BeNull();
    }

    [Fact]
    public void CreateDraft_WithNonPositiveVersionNumber_ShouldFail()
    {
        var action = () => InterviewAnswerVersion.CreateDraft(
            Guid.NewGuid(), Guid.NewGuid(), 0, "Answer", DateTimeOffset.UtcNow);

        action.Should().Throw<ArgumentOutOfRangeException>()
            .WithMessage(
                $"*{InterviewAnswerVersionErrors.InvalidVersionNumber.Message}*");
    }

    [Fact]
    public void UpdateContent_WithEquivalentContent_ShouldPreserveTimestamp()
    {
        var answer = CreateDraft();
        var originalUpdatedAtUtc = answer.UpdatedAtUtc;

        var changed = answer.UpdateContent(
            "  Original answer  ", originalUpdatedAtUtc.AddMinutes(1));

        changed.Should().BeFalse();
        answer.UpdatedAtUtc.Should().Be(originalUpdatedAtUtc);
    }

    [Fact]
    public void UpdateContent_WhenPublished_ShouldFail()
    {
        var answer = CreateDraft();
        answer.Publish(DateTimeOffset.UtcNow);

        var action = () => answer.UpdateContent(
            "Updated answer", DateTimeOffset.UtcNow.AddMinutes(1));

        action.Should().Throw<InvalidOperationException>()
            .WithMessage(InterviewAnswerVersionErrors.Published.Message);
    }

    [Fact]
    public void Publish_ShouldBeIdempotent()
    {
        var answer = CreateDraft();
        var firstPublishedAtUtc = DateTimeOffset.UtcNow;

        var firstChanged = answer.Publish(firstPublishedAtUtc);
        var secondChanged = answer.Publish(firstPublishedAtUtc.AddMinutes(5));

        firstChanged.Should().BeTrue();
        secondChanged.Should().BeFalse();
        answer.Status.Should().Be(InterviewAnswerVersionStatus.Published);
        answer.PublishedAtUtc.Should().Be(firstPublishedAtUtc);
        answer.UpdatedAtUtc.Should().Be(firstPublishedAtUtc);
    }

    private static InterviewAnswerVersion CreateDraft() =>
        InterviewAnswerVersion.CreateDraft(
            Guid.NewGuid(), Guid.NewGuid(), 1, "Original answer",
            DateTimeOffset.UtcNow);
}
