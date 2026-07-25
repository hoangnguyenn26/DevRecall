using DevRecall.Domain.Knowledge;
using FluentAssertions;

namespace DevRecall.Domain.Tests.Knowledge;

public sealed class KnowledgeNodeTests
{
    [Fact]
    public void Create_WithValidRootNode_ShouldCreateActiveRootNode()
    {
        var now = DateTimeOffset.UtcNow;
        var userId = Guid.NewGuid();

        var node = KnowledgeNode.Create(
            Guid.NewGuid(),
            userId,
            null,
            "Programming",
            0,
            now);

        node.UserId.Should().Be(userId);
        node.ParentId.Should().BeNull();
        node.Title.Should().Be("Programming");
        node.Status.Should().Be(KnowledgeNodeStatus.Active);
        node.CreatedAtUtc.Should().Be(now);
        node.UpdatedAtUtc.Should().Be(now);
    }

    [Fact]
    public void Create_WithParentId_ShouldCreateChildNode()
    {
        var parentId = Guid.NewGuid();

        var node = CreateNode(parentId: parentId);

        node.ParentId.Should().Be(parentId);
    }

    [Fact]
    public void Create_WithSurroundingWhitespace_ShouldTrimTitle()
    {
        var node = CreateNode(title: "  C#  ");

        node.Title.Should().Be("C#");
    }

    [Fact]
    public void Create_WithSelfParent_ShouldThrowInvalidOperationException()
    {
        var id = Guid.NewGuid();

        var action = () => KnowledgeNode.Create(
            id,
            Guid.NewGuid(),
            id,
            "Invalid",
            0,
            DateTimeOffset.UtcNow);

        action.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Rename_WithValidTitle_ShouldUpdateTitleAndTimestamp()
    {
        var node = CreateNode(title: "Old Title");
        var updatedAtUtc = DateTimeOffset.UtcNow.AddMinutes(1);

        node.Rename("  New Title  ", updatedAtUtc);

        node.Title.Should().Be("New Title");
        node.UpdatedAtUtc.Should().Be(updatedAtUtc);
    }

    [Fact]
    public void Archive_ShouldMarkNodeAsArchivedAndUpdateTimestamp()
    {
        var node = CreateNode();
        var archivedAtUtc = DateTimeOffset.UtcNow.AddMinutes(1);

        node.Archive(archivedAtUtc);

        node.Status.Should().Be(KnowledgeNodeStatus.Archived);
        node.UpdatedAtUtc.Should().Be(archivedAtUtc);
    }

    [Fact]
    public void MoveTo_WithAnotherParent_ShouldUpdateParentAndTimestamp()
    {
        var node = CreateNode();
        var parentId = Guid.NewGuid();
        var updatedAtUtc = DateTimeOffset.UtcNow.AddMinutes(1);

        node.MoveTo(parentId, updatedAtUtc);

        node.ParentId.Should().Be(parentId);
        node.UpdatedAtUtc.Should().Be(updatedAtUtc);
    }

    [Fact]
    public void MoveTo_WithNullParent_ShouldMoveNodeToRoot()
    {
        var node = CreateNode(parentId: Guid.NewGuid());

        node.MoveTo(null, DateTimeOffset.UtcNow);

        node.ParentId.Should().BeNull();
    }

    [Fact]
    public void MoveTo_WithSelfParent_ShouldThrowInvalidOperationException()
    {
        var node = CreateNode();

        var action = () => node.MoveTo(node.Id, DateTimeOffset.UtcNow);

        action.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void ChangePosition_ShouldUpdateParentOrderAndTimestamp()
    {
        var node = CreateNode();
        var parentId = Guid.NewGuid();
        var updatedAtUtc = DateTimeOffset.UtcNow.AddMinutes(1);

        var changed = node.ChangePosition(parentId, 2, updatedAtUtc);

        changed.Should().BeTrue();
        node.ParentId.Should().Be(parentId);
        node.SortOrder.Should().Be(2);
        node.UpdatedAtUtc.Should().Be(updatedAtUtc);
    }

    [Fact]
    public void ChangePosition_WithCurrentPosition_ShouldBeNoOp()
    {
        var node = CreateNode();
        var originalUpdatedAtUtc = node.UpdatedAtUtc;

        var changed = node.ChangePosition(
            node.ParentId,
            node.SortOrder,
            originalUpdatedAtUtc.AddMinutes(1));

        changed.Should().BeFalse();
        node.UpdatedAtUtc.Should().Be(originalUpdatedAtUtc);
    }

    [Fact]
    public void ChangePosition_WithSelfParent_ShouldThrowInvalidOperationException()
    {
        var node = CreateNode();

        var action = () => node.ChangePosition(
            node.Id,
            0,
            DateTimeOffset.UtcNow);

        action.Should().Throw<InvalidOperationException>();
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public void Create_WithEmptyTitle_ShouldThrowArgumentException(string title)
    {
        var action = () => CreateNode(title: title);

        action.Should().Throw<ArgumentException>()
            .WithParameterName(nameof(title));
    }

    [Fact]
    public void Create_WithTitleOverMaximumLength_ShouldThrowArgumentException()
    {
        var action = () => CreateNode(title: new string('a', 201));

        action.Should().Throw<ArgumentException>()
            .WithParameterName("title");
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public void Rename_WithEmptyTitle_ShouldThrowArgumentException(string title)
    {
        var node = CreateNode();

        var action = () => node.Rename(title, DateTimeOffset.UtcNow);

        action.Should().Throw<ArgumentException>()
            .WithParameterName(nameof(title));
    }

    [Fact]
    public void Rename_WithTitleOverMaximumLength_ShouldThrowArgumentException()
    {
        var node = CreateNode();

        var action = () => node.Rename(
            new string('a', 201),
            DateTimeOffset.UtcNow);

        action.Should().Throw<ArgumentException>()
            .WithParameterName("title");
    }

    [Fact]
    public void Create_WithEmptyId_ShouldThrowArgumentException()
    {
        var action = () => KnowledgeNode.Create(
            Guid.Empty,
            Guid.NewGuid(),
            null,
            "Node",
            0,
            DateTimeOffset.UtcNow);

        action.Should().Throw<ArgumentException>()
            .WithParameterName("id");
    }

    [Fact]
    public void Create_WithEmptyUserId_ShouldThrowArgumentException()
    {
        var action = () => KnowledgeNode.Create(
            Guid.NewGuid(),
            Guid.Empty,
            null,
            "Node",
            0,
            DateTimeOffset.UtcNow);

        action.Should().Throw<ArgumentException>()
            .WithParameterName("userId");
    }

    private static KnowledgeNode CreateNode(
        Guid? parentId = null,
        string title = "Node")
    {
        return KnowledgeNode.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            parentId,
            title,
            0,
            DateTimeOffset.UtcNow);
    }
}
