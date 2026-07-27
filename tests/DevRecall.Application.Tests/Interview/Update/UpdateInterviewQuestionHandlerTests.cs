using DevRecall.Application.Common.Pagination;
using DevRecall.Application.Identity;
using DevRecall.Application.Interview;
using DevRecall.Application.Interview.Update;
using DevRecall.Domain.Interview;
using FluentAssertions;

namespace DevRecall.Application.Tests.Interview.Update;

public sealed class UpdateInterviewQuestionHandlerTests
{
    [Fact]
    public async Task HandleAsync_WhenUpdateIsNoOp_ShouldNotSave()
    {
        var userId = Guid.NewGuid();
        var createdAtUtc = DateTimeOffset.UtcNow;
        var question = InterviewQuestion.Create(
            Guid.NewGuid(), userId,
            "IEnumerable vs IQueryable",
            "What is the difference?",
            "LINQ",
            InterviewQuestionDifficulty.Medium,
            null,
            createdAtUtc);
        var repository = new FakeRepository(question);
        var handler = new UpdateInterviewQuestionHandler(
            repository,
            new FakeCurrentUser(userId));

        var result = await handler.HandleAsync(
            new UpdateInterviewQuestionCommand(
                question.Id,
                "  IEnumerable   vs IQueryable ",
                " What is the difference? ",
                " LINQ ",
                "medium",
                null),
            CancellationToken.None);

        repository.SaveChangesCallCount.Should().Be(0);
        result.UpdatedAtUtc.Should().Be(createdAtUtc);
    }

    private sealed class FakeCurrentUser(Guid userId) : ICurrentUser
    {
        public bool IsAuthenticated => true;
        public Guid? UserId => userId;
    }

    private sealed class FakeRepository(InterviewQuestion question)
        : IInterviewQuestionRepository
    {
        public int SaveChangesCallCount { get; private set; }

        public Task<InterviewQuestion?> GetByIdAsync(
            Guid id,
            CancellationToken cancellationToken) =>
            Task.FromResult(id == question.Id ? question : null);

        public Task<InterviewQuestion?> GetByIdAndUserIdAsync(
            Guid id,
            Guid userId,
            CancellationToken cancellationToken) =>
            Task.FromResult(id == question.Id && userId == question.UserId
                ? question
                : null);

        public Task<PagedReadResult<InterviewQuestionListReadItem>>
            GetActiveListAsync(
                Guid userId,
                string? topic,
                InterviewQuestionDifficulty? difficulty,
                int skip,
                int take,
                CancellationToken cancellationToken) =>
            Task.FromResult(
                new PagedReadResult<InterviewQuestionListReadItem>([], 0));

        public void Add(InterviewQuestion interviewQuestion) =>
            throw new NotSupportedException();

        public Task SaveChangesAsync(CancellationToken cancellationToken)
        {
            SaveChangesCallCount++;
            return Task.CompletedTask;
        }
    }
}
