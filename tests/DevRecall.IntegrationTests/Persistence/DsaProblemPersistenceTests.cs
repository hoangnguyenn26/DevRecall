using DevRecall.Domain.Dsa;
using DevRecall.Domain.Identity;
using DevRecall.IntegrationTests.Infrastructure;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace DevRecall.IntegrationTests.Persistence;

[Collection(PostgreSqlTestSuite.Name)]
public sealed class DsaProblemPersistenceTests(PostgreSqlFixture fixture)
{
    [Fact]
    public async Task ShouldPersistDsaProblemWithTopics()
    {
        await using var context = fixture.CreateDbContext();
        var user = CreateUser();
        var problem = CreateProblem(user.Id);
        context.Users.Add(user);
        context.DsaProblems.Add(problem);

        await context.SaveChangesAsync(CancellationToken.None);
        context.ChangeTracker.Clear();

        var persisted = await context.DsaProblems
            .AsNoTracking()
            .Include(item => item.Topics)
            .SingleAsync(
                item => item.Id == problem.Id, CancellationToken.None);

        persisted.Title.Should().Be("Two Sum");
        persisted.Difficulty.Should().Be(DsaProblemDifficulty.Easy);
        persisted.Topics.Select(topic => topic.Name)
            .Should().BeEquivalentTo(["Array", "Hash Table"]);
    }

    [Fact]
    public async Task ShouldReplaceProblemTopics()
    {
        await using var context = fixture.CreateDbContext();
        var user = CreateUser();
        var problem = CreateProblem(user.Id);
        context.Users.Add(user);
        context.DsaProblems.Add(problem);
        await context.SaveChangesAsync(CancellationToken.None);

        problem.ReplaceTopics(
            ["Array", "Two Pointers"], DateTimeOffset.UtcNow.AddMinutes(1));
        await context.SaveChangesAsync(CancellationToken.None);
        context.ChangeTracker.Clear();

        var persisted = await context.DsaProblems
            .AsNoTracking()
            .Include(item => item.Topics)
            .SingleAsync(
                item => item.Id == problem.Id, CancellationToken.None);

        persisted.Topics.Select(topic => topic.Name)
            .Should().BeEquivalentTo(["Array", "Two Pointers"]);
    }

    private static DsaProblem CreateProblem(Guid userId) =>
        DsaProblem.Create(
            Guid.NewGuid(), userId, "Two Sum",
            "Find two numbers that add up to target.",
            DsaProblemDifficulty.Easy, "LeetCode",
            "https://leetcode.com/problems/two-sum/",
            ["Array", "Hash Table"], DateTimeOffset.UtcNow);

    private static User CreateUser()
    {
        var suffix = Guid.NewGuid().ToString("N");
        return User.Create(
            Guid.NewGuid(), $"dsa-{suffix}@example.com", "DSA User",
            "test-password-hash", DateTimeOffset.UtcNow);
    }
}
