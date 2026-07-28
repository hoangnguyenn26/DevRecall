using DevRecall.Domain.Dsa;
using DevRecall.Domain.Dsa.Attempts;
using DevRecall.Domain.Identity;
using DevRecall.IntegrationTests.Infrastructure;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace DevRecall.IntegrationTests.Persistence;

[Collection(PostgreSqlTestSuite.Name)]
public sealed class DsaAttemptPersistenceTests(PostgreSqlFixture fixture)
{
    [Fact]
    public async Task ShouldPersistDsaAttempt()
    {
        await using var context = fixture.CreateDbContext();
        var user = CreateUser();
        var problem = CreateProblem(user.Id);
        var attempt = CreateAttempt(problem.Id, 1);
        context.Users.Add(user);
        context.DsaProblems.Add(problem);
        context.DsaAttempts.Add(attempt);

        await context.SaveChangesAsync(CancellationToken.None);
        context.ChangeTracker.Clear();

        var persisted = await context.DsaAttempts
            .AsNoTracking()
            .SingleAsync(
                item => item.Id == attempt.Id, CancellationToken.None);

        persisted.AttemptNumber.Should().Be(1);
        persisted.Result.Should().Be(DsaAttemptResult.Failed);
        persisted.Language.Should().Be("C#");
        persisted.SolutionCode.Should().Contain("public int[] TwoSum");
        persisted.Approach.Should().Be("Tried a hash map approach.");
    }

    [Fact]
    public async Task ShouldRejectDuplicateAttemptNumberForSameProblem()
    {
        await using var context = fixture.CreateDbContext();
        var user = CreateUser();
        var problem = CreateProblem(user.Id);
        context.Users.Add(user);
        context.DsaProblems.Add(problem);
        context.DsaAttempts.AddRange(
            CreateAttempt(problem.Id, 1),
            CreateAttempt(problem.Id, 1, DsaAttemptResult.Solved));

        var action = async () =>
            await context.SaveChangesAsync(CancellationToken.None);

        await action.Should().ThrowAsync<DbUpdateException>();
    }

    [Fact]
    public async Task ShouldAllowSameAttemptNumberForDifferentProblems()
    {
        await using var context = fixture.CreateDbContext();
        var user = CreateUser();
        var firstProblem = CreateProblem(user.Id);
        var secondProblem = CreateProblem(user.Id, "Three Sum");
        context.Users.Add(user);
        context.DsaProblems.AddRange(firstProblem, secondProblem);
        context.DsaAttempts.AddRange(
            CreateAttempt(firstProblem.Id, 1),
            CreateAttempt(secondProblem.Id, 1));

        await context.SaveChangesAsync(CancellationToken.None);

        var attemptCount = await context.DsaAttempts.CountAsync(
            attempt => attempt.DsaProblemId == firstProblem.Id
                || attempt.DsaProblemId == secondProblem.Id);
        attemptCount.Should().Be(2);
    }

    private static DsaAttempt CreateAttempt(
        Guid problemId,
        int attemptNumber,
        DsaAttemptResult result = DsaAttemptResult.Failed) =>
        DsaAttempt.Create(
            Guid.NewGuid(), problemId, attemptNumber, result, "C#",
            """
            public int[] TwoSum(int[] nums, int target)
            {
                return [];
            }
            """,
            "Tried a hash map approach.", "O(n)", "O(n)", 35,
            "Forgot to insert the current value.",
            DateTimeOffset.UtcNow, DateTimeOffset.UtcNow);

    private static DsaProblem CreateProblem(
        Guid userId, string title = "Two Sum") =>
        DsaProblem.Create(
            Guid.NewGuid(), userId, title, "Find the answer.",
            DsaProblemDifficulty.Easy, "LeetCode", null, ["Array"],
            DateTimeOffset.UtcNow);

    private static User CreateUser()
    {
        var suffix = Guid.NewGuid().ToString("N");
        return User.Create(
            Guid.NewGuid(), $"attempt-{suffix}@example.com", "Attempt User",
            "test-password-hash", DateTimeOffset.UtcNow);
    }
}
