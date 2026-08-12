using DevRecall.Domain.LearningContent;
using FluentAssertions;

namespace DevRecall.Domain.Tests.LearningContent;

public sealed class LearningContentProgressTests
{
    private static readonly DateTimeOffset Now = new(2026, 8, 13, 1, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Start_ShouldCreateInProgressVersionOne()
    {
        var progress = LearningContentProgress.Start(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Now);
        progress.Status.Should().Be(LearningProgressStatus.InProgress);
        progress.StartedAtUtc.Should().Be(Now);
        progress.CompletedAtUtc.Should().BeNull();
        progress.Version.Should().Be(1);
    }

    [Fact]
    public void Complete_ShouldTransitionOnceAndKeepCompletionImmutable()
    {
        var progress = LearningContentProgress.Start(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Now);
        progress.Complete(1, Now.AddMinutes(5)).Should().BeTrue();
        progress.Complete(1, Now.AddMinutes(10)).Should().BeFalse();
        progress.CompletedAtUtc.Should().Be(Now.AddMinutes(5));
        progress.Version.Should().Be(2);
    }

    [Fact]
    public void Complete_ShouldRejectStaleVersionBeforeFinalState()
    {
        var progress = LearningContentProgress.Start(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Now);
        var action = () => progress.Complete(2, Now.AddMinutes(5));
        action.Should().Throw<InvalidOperationException>()
            .WithMessage("LEARNING_CONTENT_PROGRESS_CONFLICT");
    }
}
