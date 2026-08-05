using DevRecall.Domain.Knowledge;
using FluentAssertions;

namespace DevRecall.Domain.Tests.Knowledge;

public sealed class KnowledgeNodeVersionTests
{
    [Fact]
    public void Update_WithMultipleChanges_ShouldIncrementVersionOnce()
    {
        var now = DateTimeOffset.UtcNow;
        var node = KnowledgeNode.Create(Guid.NewGuid(), Guid.NewGuid(), null, "Original", 0, now);

        var changed = node.Update(" Updated ", " Content ", Guid.NewGuid(), [],
            [Guid.NewGuid()], now.AddMinutes(1));

        changed.Should().BeTrue();
        node.Version.Should().Be(2);
        node.Title.Should().Be("Updated");
        node.Content.Should().Be("Content");
    }

    [Fact]
    public void Update_WithNormalizedEquivalentStateAndReorderedTags_ShouldBeNoOp()
    {
        var now = DateTimeOffset.UtcNow;
        var firstTag = Guid.NewGuid(); var secondTag = Guid.NewGuid();
        var node = KnowledgeNode.Create(Guid.NewGuid(), Guid.NewGuid(), null, "Title", 0, now);

        var changed = node.Update(" Title ", "  ", null,
            [firstTag, secondTag], [secondTag, firstTag, firstTag], now.AddMinutes(1));

        changed.Should().BeFalse();
        node.Version.Should().Be(1);
        node.UpdatedAtUtc.Should().Be(now);
    }
}
