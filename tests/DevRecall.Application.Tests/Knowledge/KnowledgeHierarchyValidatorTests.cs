using DevRecall.Application.Common.Exceptions;
using DevRecall.Application.Knowledge;
using FluentAssertions;

namespace DevRecall.Application.Tests.Knowledge;

public sealed class KnowledgeHierarchyValidatorTests
{
    [Fact]
    public void EnsureNoCycle_WithNullParent_ShouldSucceed()
    {
        var action = () => KnowledgeHierarchyValidator.EnsureNoCycle(
            Guid.NewGuid(),
            null,
            []);

        action.Should().NotThrow();
    }

    [Fact]
    public void EnsureNoCycle_WithSelfParent_ShouldThrowConflictException()
    {
        var nodeId = Guid.NewGuid();

        var action = () => KnowledgeHierarchyValidator.EnsureNoCycle(
            nodeId,
            nodeId,
            []);

        AssertCircularHierarchy(action);
    }

    [Fact]
    public void EnsureNoCycle_WithDirectDescendant_ShouldThrowConflictException()
    {
        var rootId = Guid.NewGuid();
        var childId = Guid.NewGuid();
        KnowledgeNodeHierarchyItem[] hierarchy =
        [
            new(rootId, null),
            new(childId, rootId)
        ];

        var action = () => KnowledgeHierarchyValidator.EnsureNoCycle(
            rootId,
            childId,
            hierarchy);

        AssertCircularHierarchy(action);
    }

    [Fact]
    public void EnsureNoCycle_WithDeepDescendant_ShouldThrowConflictException()
    {
        var rootId = Guid.NewGuid();
        var childId = Guid.NewGuid();
        var grandchildId = Guid.NewGuid();
        var deepestId = Guid.NewGuid();
        KnowledgeNodeHierarchyItem[] hierarchy =
        [
            new(rootId, null),
            new(childId, rootId),
            new(grandchildId, childId),
            new(deepestId, grandchildId)
        ];

        var action = () => KnowledgeHierarchyValidator.EnsureNoCycle(
            rootId,
            deepestId,
            hierarchy);

        AssertCircularHierarchy(action);
    }

    [Fact]
    public void EnsureNoCycle_WithLegalSiblingMove_ShouldSucceed()
    {
        var rootId = Guid.NewGuid();
        var firstChildId = Guid.NewGuid();
        var secondChildId = Guid.NewGuid();
        KnowledgeNodeHierarchyItem[] hierarchy =
        [
            new(rootId, null),
            new(firstChildId, rootId),
            new(secondChildId, rootId)
        ];

        var action = () => KnowledgeHierarchyValidator.EnsureNoCycle(
            firstChildId,
            secondChildId,
            hierarchy);

        action.Should().NotThrow();
    }

    [Fact]
    public void EnsureNoCycle_WithExistingCorruptCycle_ShouldThrowConflictException()
    {
        var nodeId = Guid.NewGuid();
        var firstId = Guid.NewGuid();
        var secondId = Guid.NewGuid();
        KnowledgeNodeHierarchyItem[] hierarchy =
        [
            new(nodeId, null),
            new(firstId, secondId),
            new(secondId, firstId)
        ];

        var action = () => KnowledgeHierarchyValidator.EnsureNoCycle(
            nodeId,
            firstId,
            hierarchy);

        AssertCircularHierarchy(action);
    }

    private static void AssertCircularHierarchy(Action action)
    {
        var exception = action.Should().Throw<ConflictException>();
        exception.Which.ErrorCode.Should()
            .Be("KNOWLEDGE_CIRCULAR_HIERARCHY");
    }
}
