using DevRecall.Domain.Identity;
using DevRecall.Domain.Interview;
using DevRecall.IntegrationTests.Infrastructure;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace DevRecall.IntegrationTests.Persistence;

[Collection(PostgreSqlTestSuite.Name)]
public sealed class InterviewQuestionPersistenceTests(PostgreSqlFixture fixture)
{
    [Fact]
    public async Task ShouldPersistInterviewQuestion()
    {
        await using var context = fixture.CreateDbContext();
        var user = CreateUser();
        var question = InterviewQuestion.Create(
            Guid.NewGuid(), user.Id,
            "IEnumerable vs IQueryable",
            "What is the difference between them?",
            "LINQ",
            InterviewQuestionDifficulty.Medium,
            "Focus on query execution.",
            DateTimeOffset.UtcNow);

        context.Users.Add(user);
        context.InterviewQuestions.Add(question);
        await context.SaveChangesAsync(CancellationToken.None);
        context.ChangeTracker.Clear();

        var persisted = await context.InterviewQuestions.SingleAsync(
            item => item.Id == question.Id,
            CancellationToken.None);

        persisted.Title.Should().Be("IEnumerable vs IQueryable");
        persisted.Question.Should().Be("What is the difference between them?");
        persisted.Topic.Should().Be("LINQ");
        persisted.Difficulty.Should().Be(InterviewQuestionDifficulty.Medium);
        persisted.Notes.Should().Be("Focus on query execution.");
        persisted.Status.Should().Be(InterviewQuestionStatus.Active);
    }

    [Fact]
    public async Task ShouldRejectQuestionWithMissingUser()
    {
        await using var context = fixture.CreateDbContext();
        var question = InterviewQuestion.Create(
            Guid.NewGuid(), Guid.NewGuid(),
            "Dependency Injection",
            "What is dependency injection?",
            "Software Design",
            InterviewQuestionDifficulty.Easy,
            null,
            DateTimeOffset.UtcNow);

        context.InterviewQuestions.Add(question);

        var action = async () =>
            await context.SaveChangesAsync(CancellationToken.None);

        await action.Should().ThrowAsync<DbUpdateException>();
    }

    private static User CreateUser()
    {
        var suffix = Guid.NewGuid().ToString("N");
        return User.Create(
            Guid.NewGuid(),
            $"interview-{suffix}@example.com",
            "Interview User",
            "test-password-hash",
            DateTimeOffset.UtcNow);
    }
}
