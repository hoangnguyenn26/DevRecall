using DevRecall.Domain.Interview;
using FluentAssertions;

namespace DevRecall.Domain.Tests.Interview;

public sealed class InterviewQuestionTests
{
    [Fact]
    public void Create_ShouldNormalizeQuestionFields()
    {
        var question = InterviewQuestion.Create(
            Guid.NewGuid(), Guid.NewGuid(),
            "  IEnumerable   vs IQueryable ",
            "  What is the difference between them?  ",
            "  LINQ  ",
            InterviewQuestionDifficulty.Medium,
            "  Focus on execution location.  ",
            DateTimeOffset.UtcNow);

        question.Title.Should().Be("IEnumerable vs IQueryable");
        question.Question.Should().Be("What is the difference between them?");
        question.Topic.Should().Be("LINQ");
        question.Notes.Should().Be("Focus on execution location.");
    }

    [Fact]
    public void Update_WithSameNormalizedValues_ShouldBeNoOp()
    {
        var createdAtUtc = DateTimeOffset.UtcNow;
        var question = CreateQuestion(createdAtUtc);

        var changed = question.Update(
            "  IEnumerable   vs IQueryable ",
            " What is the difference? ",
            " LINQ ",
            InterviewQuestionDifficulty.Medium,
            null,
            createdAtUtc.AddMinutes(5));

        changed.Should().BeFalse();
        question.UpdatedAtUtc.Should().Be(createdAtUtc);
    }

    [Fact]
    public void Update_WhenQuestionIsArchived_ShouldFail()
    {
        var question = CreateQuestion(DateTimeOffset.UtcNow);
        question.Archive(DateTimeOffset.UtcNow.AddMinutes(1));

        var action = () => question.Update(
            "Updated title", "Updated question", "C#",
            InterviewQuestionDifficulty.Hard, null,
            DateTimeOffset.UtcNow.AddMinutes(2));

        action.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Archive_WhenAlreadyArchived_ShouldBeIdempotent()
    {
        var question = CreateQuestion(DateTimeOffset.UtcNow);
        var archivedAtUtc = question.UpdatedAtUtc.AddMinutes(1);

        question.Archive(archivedAtUtc);
        var changed = question.Archive(archivedAtUtc.AddMinutes(1));

        changed.Should().BeFalse();
        question.Status.Should().Be(InterviewQuestionStatus.Archived);
        question.UpdatedAtUtc.Should().Be(archivedAtUtc);
    }

    private static InterviewQuestion CreateQuestion(DateTimeOffset createdAtUtc) =>
        InterviewQuestion.Create(
            Guid.NewGuid(), Guid.NewGuid(),
            "IEnumerable vs IQueryable",
            "What is the difference?",
            "LINQ",
            InterviewQuestionDifficulty.Medium,
            null,
            createdAtUtc);
}
