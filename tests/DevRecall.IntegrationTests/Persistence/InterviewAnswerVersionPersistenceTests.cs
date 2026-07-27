using DevRecall.Domain.Identity;
using DevRecall.Domain.Interview;
using DevRecall.Domain.Interview.Answers;
using DevRecall.IntegrationTests.Infrastructure;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace DevRecall.IntegrationTests.Persistence;

[Collection(PostgreSqlTestSuite.Name)]
public sealed class InterviewAnswerVersionPersistenceTests(
    PostgreSqlFixture fixture)
{
    [Fact]
    public async Task ShouldPersistAnswerVersion()
    {
        await using var context = fixture.CreateDbContext();
        var user = CreateUser();
        var question = CreateQuestion(user.Id);
        var answer = InterviewAnswerVersion.CreateDraft(
            Guid.NewGuid(), question.Id, 1,
            "IEnumerable works with delegates.", DateTimeOffset.UtcNow);

        context.Users.Add(user);
        context.InterviewQuestions.Add(question);
        context.InterviewAnswerVersions.Add(answer);
        await context.SaveChangesAsync(CancellationToken.None);
        context.ChangeTracker.Clear();

        var persisted = await context.InterviewAnswerVersions.SingleAsync(
            item => item.Id == answer.Id,
            CancellationToken.None);

        persisted.VersionNumber.Should().Be(1);
        persisted.Content.Should().Be("IEnumerable works with delegates.");
        persisted.Status.Should().Be(InterviewAnswerVersionStatus.Draft);
        persisted.PublishedAtUtc.Should().BeNull();
    }

    [Fact]
    public async Task ShouldRejectDuplicateVersionNumber()
    {
        await using var context = fixture.CreateDbContext();
        var user = CreateUser();
        var question = CreateQuestion(user.Id);
        var first = InterviewAnswerVersion.CreateDraft(
            Guid.NewGuid(), question.Id, 1, "First answer",
            DateTimeOffset.UtcNow);
        first.Publish(DateTimeOffset.UtcNow);
        var duplicate = InterviewAnswerVersion.CreateDraft(
            Guid.NewGuid(), question.Id, 1, "Duplicate answer",
            DateTimeOffset.UtcNow);

        context.Users.Add(user);
        context.InterviewQuestions.Add(question);
        context.InterviewAnswerVersions.AddRange(first, duplicate);

        var action = async () =>
            await context.SaveChangesAsync(CancellationToken.None);

        await action.Should().ThrowAsync<DbUpdateException>();
    }

    [Fact]
    public async Task ShouldRejectSecondDraftForSameQuestion()
    {
        await using var context = fixture.CreateDbContext();
        var user = CreateUser();
        var question = CreateQuestion(user.Id);
        var firstDraft = InterviewAnswerVersion.CreateDraft(
            Guid.NewGuid(), question.Id, 1, "First draft",
            DateTimeOffset.UtcNow);
        var secondDraft = InterviewAnswerVersion.CreateDraft(
            Guid.NewGuid(), question.Id, 2, "Second draft",
            DateTimeOffset.UtcNow);

        context.Users.Add(user);
        context.InterviewQuestions.Add(question);
        context.InterviewAnswerVersions.AddRange(firstDraft, secondDraft);

        var action = async () =>
            await context.SaveChangesAsync(CancellationToken.None);

        await action.Should().ThrowAsync<DbUpdateException>();
    }

    private static InterviewQuestion CreateQuestion(Guid userId) =>
        InterviewQuestion.Create(
            Guid.NewGuid(), userId, "IEnumerable vs IQueryable",
            "What is the difference?", "LINQ",
            InterviewQuestionDifficulty.Medium, null, DateTimeOffset.UtcNow);

    private static User CreateUser()
    {
        var suffix = Guid.NewGuid().ToString("N");
        return User.Create(
            Guid.NewGuid(), $"answer-{suffix}@example.com", "Answer User",
            "test-password-hash", DateTimeOffset.UtcNow);
    }
}
