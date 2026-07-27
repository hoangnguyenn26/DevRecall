using DevRecall.Domain.Dsa;
using FluentAssertions;

namespace DevRecall.Domain.Tests.Dsa;

public sealed class DsaProblemTests
{
    [Fact]
    public void Create_ShouldNormalizeFieldsAndTopics()
    {
        var problem = DsaProblem.Create(
            Guid.NewGuid(), Guid.NewGuid(), "  Two   Sum ",
            "  First line.\n\nSecond line.  ",
            DsaProblemDifficulty.Easy, "  LeetCode ",
            "https://leetcode.com/problems/two-sum/",
            [" Array ", "array", " Hash   Table "],
            DateTimeOffset.UtcNow);

        problem.Title.Should().Be("Two Sum");
        problem.Description.Should().Be("First line.\n\nSecond line.");
        problem.Source.Should().Be("LeetCode");
        problem.Topics.Select(topic => topic.Name)
            .Should().Equal("Array", "Hash Table");
    }

    [Fact]
    public void Create_WithDuplicateTopics_ShouldRemoveDuplicatesCaseInsensitively()
    {
        var problem = CreateProblem(["Array", " array ", "HASH TABLE", "Hash Table"]);

        problem.Topics.Select(topic => topic.NormalizedName)
            .Should().Equal("ARRAY", "HASH TABLE");
    }

    [Fact]
    public void Update_WithEquivalentNormalizedData_ShouldBeNoOp()
    {
        var createdAtUtc = DateTimeOffset.UtcNow;
        var problem = CreateProblem(
            ["Array", "Hash Table"], createdAtUtc);

        var changed = problem.Update(
            "  Two   Sum ", " Find two numbers. ",
            DsaProblemDifficulty.Easy, " LeetCode ", null,
            ["hash table", "array"], createdAtUtc.AddMinutes(5));

        changed.Should().BeFalse();
        problem.UpdatedAtUtc.Should().Be(createdAtUtc);
    }

    [Fact]
    public void Update_WhenArchived_ShouldThrow()
    {
        var problem = CreateProblem();
        problem.Archive(DateTimeOffset.UtcNow.AddMinutes(1));

        var action = () => problem.Update(
            problem.Title, problem.Description, problem.Difficulty,
            problem.Source, problem.ExternalUrl, [],
            DateTimeOffset.UtcNow.AddMinutes(2));

        action.Should().Throw<InvalidOperationException>()
            .WithMessage(DsaProblemErrors.Archived.Message);
    }

    [Fact]
    public void Archive_WhenAlreadyArchived_ShouldBeIdempotent()
    {
        var problem = CreateProblem();
        var archivedAtUtc = DateTimeOffset.UtcNow.AddMinutes(1);

        problem.Archive(archivedAtUtc).Should().BeTrue();
        problem.Archive(archivedAtUtc.AddMinutes(1)).Should().BeFalse();
        problem.UpdatedAtUtc.Should().Be(archivedAtUtc);
    }

    private static DsaProblem CreateProblem(
        IEnumerable<string>? topics = null,
        DateTimeOffset? createdAtUtc = null) =>
        DsaProblem.Create(
            Guid.NewGuid(), Guid.NewGuid(), "Two Sum", "Find two numbers.",
            DsaProblemDifficulty.Easy, "LeetCode", null, topics,
            createdAtUtc ?? DateTimeOffset.UtcNow);
}
